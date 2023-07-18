using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Screech;
using Screenplay.Variables;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Screenplay.Editor
{
    [CustomEditor(typeof(ScenarioImporter))]
    public class ScenarioEditor : ScriptedImporterEditor
    {
        const string ComponentTooltip = "Should this be implemented scene-side ?";
        const string HelpMessage = @"Syntax:
// A double slash means that this line will not show up, you can leave yourself notes through these
> A choice
    This incremented line will play only if the above choice is selected by the player
> Another choice
<
// The above explicitly closes this set of choices, for when you have multiple set of choices next to each other like the one below
> This is a choice that will be shown after the first set of choice

Some text {a command}rest of the text
// Runs the command 'a command' right after 'Some text ' is shown on screen

=== A Scope ===
// The above is a scope, those aren't displayed but are used to split up your text and also as targets for -> jumps

-> A Scope
// Jump to that scope, continue reading from there
<-
// Continue from wherever we jumped from or exit prematurely if we didn't jump yet";

        static readonly GUIContent _tmpContent = new();
        static readonly BlockingCollection<(FormattableString str, ScenarioEditor drawer)> _jobColl = new();
        static GUIStyle __style, __styleNoText;
        static int _bootThread;

        readonly string _textEditorFocusName;
        Scenario __scenario;
        bool _showBindingsPanel;
        Vector2 _bindingsScroll;
        bool _changed;
        float _editorStartCache;
        [NonSerialized] string _fileContent;
        int? _forceRefocus;
        string _keyOnEditorCursor;
        int _lastCursorIndex;
        float _lastCursorY;
        int _lastVersion;
        bool _scheduleBind;
        SerializedObject _scriptSO;
        Vector2 _scrollView;
        Exception _validationIssue;

        public ScenarioEditor()
        {
            _textEditorFocusName = $"{GetType().FullName}:{Guid.NewGuid()}";
        }

        static GUIStyle _style => __style ??= new GUIStyle(EditorStyles.textArea)
        {
            richText = true,
            fontSize = 14,
            wordWrap = false
        };

        static GUIStyle _styleNoText => __styleNoText ??= new GUIStyle(EditorStyles.textArea)
        {
            richText = true,
            fontSize = 14,
            onNormal = { textColor = default },
            onActive = { textColor = default },
            onFocused = { textColor = default },
            onHover = { textColor = default },
            normal = { textColor = default },
            active = { textColor = default },
            focused = { textColor = default },
            hover = { textColor = default },
            wordWrap = false
        };

        public int FontSize
        {
            get => _style.fontSize;
            set
            {
                GUIStyle styleNoText = _styleNoText;
                int fontSize = _style.fontSize = value;
                styleNoText.fontSize = fontSize;
            }
        }

        new ScenarioImporter target => (ScenarioImporter)base.target;
        Scenario _scenario => __scenario ??= target.Scenario;

        public override bool showImportedObject => false;

        public override void OnEnable()
        {
            base.OnEnable();
            Validate();
        }

        public override bool UseDefaultMargins() => false;

        void Validate()
        {
            _lastVersion = _scenario.Version;
            try
            {
                _scenario.ValidateAll();
                FormattableString formattedString = _scenario.AsFormattedString(false, Span<BindingOverride>.Empty);
                _jobColl.Add((formattedString, this));
                if (Interlocked.Exchange(ref _bootThread, 1) == 0)
                    Task.Run(ParsingValidationThread);
            }
            catch (ValidationException e)
            {
                _validationIssue = e;
            }
        }

        public override void OnInspectorGUI()
        {
            ApplyRevertGUI();
            serializedObject.Update();
            _scenario.hideFlags &= ~HideFlags.NotEditable;
            _scriptSO ??= new SerializedObject(_scenario);
            _scriptSO.Update();

            if (_validationIssue is ParseWarnings)
                GUI.backgroundColor = Color.Lerp(Color.yellow, GUI.backgroundColor, 0.75f);
            else if (_validationIssue != null)
                GUI.backgroundColor = Color.Lerp(Color.red, GUI.backgroundColor, 0.85f);

            DrawToolbar();

            Rect lastRect = GUILayoutUtility.GetLastRect();
            _editorStartCache = lastRect.y == 0f ? _editorStartCache : lastRect.y;
            float heightAvailable = Screen.height - _editorStartCache - 179f/*Account for the Asset Labels scope*/;

            bool dataChanged;
            using (new Horizontal()) // Text editor & Bindings panel
            {
                using (new Vertical()) // Text editor
                {
                    _tmpContent.text = _scenario.Content;
                    float editorHeight = _style.CalcHeight(_tmpContent, EditorGUIUtility.currentViewWidth);
                    int width = _showBindingsPanel ? Screen.width / 2 : Screen.width;

                    using(new ScrollView(ref _scrollView, GUILayout.Height(heightAvailable), GUILayout.Width(width)))
                    {
                        Rect textEditorRect = EditorGUILayout.GetControlRect(false, GUILayout.Height(editorHeight), GUILayout.ExpandHeight(true));
                        textEditorRect.x -= 4f;
                        textEditorRect.width += 8f;
                        textEditorRect.height += 4f;

                        EditorGUI.BeginChangeCheck();
                        DrawBindingsOverlay(_scriptSO, _scenario, textEditorRect);
                        dataChanged = EditorGUI.EndChangeCheck();

                        DrawEditor(textEditorRect, _scenario);

                        EditorGUI.BeginChangeCheck();
                        DrawBindingsOverlay(_scriptSO, _scenario, textEditorRect);
                        dataChanged |= EditorGUI.EndChangeCheck();
                    }
                }

                using (new Vertical()) // Bindings panel
                {
                    EditorGUI.BeginChangeCheck();
                    DrawBindingsPanel(_scenario, _scriptSO);
                    dataChanged |= EditorGUI.EndChangeCheck();
                }
            }

            DrawErrorOverlay(/*lastRect.y + lastRect.height*/32f, Screen.width-12f);

            serializedObject.ApplyModifiedProperties();
            _scriptSO.ApplyModifiedProperties();
            if (dataChanged || _lastVersion != _scenario.Version)
            {
                EditorUtility.SetDirty(target);
                _changed = true;
                Validate();
            }
        }

        public override bool HasModified()
        {
            if (_fileContent == null)
            {
                string dir = Path.GetDirectoryName(Application.dataPath);
                string path = Path.Combine(dir, AssetDatabase.GetAssetPath(target));
                _fileContent = File.ReadAllText(path);
            }

            return _changed || !string.Equals(_fileContent, _scenario.Content, StringComparison.Ordinal);
        }

        protected override void Apply()
        {
            _fileContent = null;
            _changed = false;
            base.Apply();
            string dir = Path.GetDirectoryName(Application.dataPath);
            string path = Path.Combine(dir, AssetDatabase.GetAssetPath(target));
            File.WriteAllText(path, target.Scenario.Content);
            target.Bindings = target.Scenario.Bindings;
            target.SaveAndReimport();
            _lastVersion = target.Scenario.Version;
        }

        public override void DiscardChanges()
        {
            base.DiscardChanges();
            string dir = Path.GetDirectoryName(Application.dataPath);
            string path = Path.Combine(dir, AssetDatabase.GetAssetPath(target));
            _scenario.Content = File.ReadAllText(path).Replace("\r", "");
        }

        static void ParsingValidationThread()
        {
            try
            {
                while (true)
                {
                    (FormattableString str, ScenarioEditor drawer) = _jobColl.Take();
                    try
                    {
                        ParseWarnings warnings = new();
                        Script.Parse(str, delegate(Issue x) { warnings.Append(x); });
                        drawer._validationIssue = warnings.Issues.Count > 0 ? warnings : null;
                    }
                    catch (Exception e2)
                    {
                        Debug.LogException(e2);
                    }
                }
            }
            catch (Exception e) when (e is not ThreadAbortException)
            {
                Debug.LogException(e);
            }
        }

        static string SyntaxHighlighting(string s, [CanBeNull] ParseWarnings warnings)
        {
            string output = Script.CloseChoice.Replace(s, match => $"<color=#aaaaff66>{match}</color>");
            output = Script.Choice.Replace(output, match => $"<color=#aaaaffff>{match}</color>");
            output = Script.Comment.Replace(output, match => $"<color=#ffffff22>{match}</color>");
            output = Scenario.CommandPattern.Replace(output, match => $"<color=#00ffffff>{match}</color>");
            output = Script.GoTo.Replace(output, match => $"<color=#00aa00ff>{match}</color>");
            output = Script.Return.Replace(output, match => $"<color=#00aa00ff>{match}</color>");
            output = Script.Scope.Replace(output, match => $"<color=#00aa00ff>{match}</color>");

            if (warnings != null)
            {
                foreach (Issue issue in warnings.Issues) // Mark syntax issue red, ensures that existing syntax color is overwritten
                {
                    int index = -1;
                    for (int line = 0; line < issue.SourceLine; line++)
                        index = output.IndexOf('\n', index + 1);

                    if (output.AsSpan()[(index + 1)..].StartsWith("<color="))
                        index = output.IndexOf('>', index + 1);

                    output = output.Insert(index + 1, "<color=#ff0000ff>");
                    int endIndex = output.IndexOf('\n', index + 1);
                    endIndex = endIndex == -1 ? output.Length : endIndex;
                    output = output.Insert(endIndex, "</color>");
                }
            }

            return output;
        }

        void DrawToolbar()
        {
            Rect positionCpy = GUILayoutUtility.GetLastRect();
            positionCpy.position = new Vector2(0, 8);
            positionCpy.size = new Vector2(Screen.width-120, 18);
            GUILayout.BeginArea(positionCpy);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Help"))
            {
                string highlightedHelpMessage = SyntaxHighlighting(HelpMessage, null);
                ModalWindow.New(window =>
                {
                    window.titleContent = new GUIContent("Help");
                    Rect r = window.position;
                    r.position = default;
                    GUI.TextArea(r, highlightedHelpMessage, _style);
                });
            }

            using (new GUIBackgroundColorScope(_styleNoText.richText ? GUI.backgroundColor : GUI.backgroundColor * 0.5f))
            {
                if (GUILayout.Button("Rich Text"))
                {
                    bool richText = _styleNoText.richText = !_styleNoText.richText;
                    _style.richText = richText;
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("-"))
                FontSize--;
            if (GUILayout.Button("+"))
                FontSize++;
            if (GUILayout.Button("1:1"))
                FontSize = EditorStyles.textArea.fontSize;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Maximize"))
                EditorWindow.mouseOverWindow.maximized = !EditorWindow.mouseOverWindow.maximized;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Test"))
            {
                TestScenarioStub stub = CreateInstance<TestScenarioStub>();
                stub.ScenarioRef = _scenario;
                if(EditorApplication.isPlaying == false)
                    EditorApplication.isPlaying = true;
                else
                    AssetVariableManagement.RollbackVariables?.Invoke();
                stub.maxSize = Vector2.one;
                stub.Show();
            }

            GUILayout.FlexibleSpace();

            using (new GUIBackgroundColorScope(_showBindingsPanel ? GUI.backgroundColor * 0.5f : GUI.backgroundColor))
            {
                if (GUILayout.Button(_showBindingsPanel ? "«Bindings" : "»Bindings"))
                    _showBindingsPanel = !_showBindingsPanel;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void DrawEditor(Rect textEditorRect, Scenario script)
        {
            ref string content = ref script.Content;
            TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            if (GUI.GetNameOfFocusedControl() == _textEditorFocusName && _lastCursorIndex != editor.cursorIndex)
            {
                _lastCursorIndex = editor.cursorIndex;
                _keyOnEditorCursor = null;
                _lastCursorY = editor.graphicalCursorPos.y;
            }

            // Special handling for copying, the editor doesn't copy tags out of the box
            if (GUI.GetNameOfFocusedControl() == _textEditorFocusName
                && Event.current.type == EventType.KeyUp
                && Event.current.modifiers is EventModifiers.Control or EventModifiers.Command
                && Event.current.keyCode == KeyCode.C)
            {
                Event.current.Use();
                EditorGUIUtility.systemCopyBuffer = editor.SelectedText;
            }

            if (_forceRefocus.HasValue)
            {
                EditorGUI.FocusTextInControl(_textEditorFocusName);
                if (editor.text == content)
                {
                    if (editor.cursorIndex != _forceRefocus.Value)
                    {
                        int selectIndex = editor.cursorIndex = _forceRefocus.Value;
                        editor.selectIndex = selectIndex;
                    }
                    else if (Event.current.keyCode == KeyCode.Tab && Event.current.type == EventType.KeyUp)
                        _forceRefocus = null;
                }
            }

            if (GUI.GetNameOfFocusedControl() == _textEditorFocusName && Event.current.keyCode == KeyCode.Tab && Event.current.type == EventType.KeyDown)
            {
                Event.current.Use();
                if (Event.current.shift)
                {
                    int i = Math.Max(content.LastIndexOf('\n', editor.cursorIndex - 1), content.LastIndexOf('\t', editor.cursorIndex));
                    if (i == -1 || content[i] == '\n')
                        _forceRefocus = Math.Max(i, 0);
                    else
                    {
                        if (i != editor.cursorIndex)
                            _forceRefocus = editor.cursorIndex - 1;
                        content = content.Remove(i, 1);
                    }
                }
                else
                {
                    content = content.Insert(editor.cursorIndex, "\t");
                    _forceRefocus = editor.cursorIndex + 1;
                }
            }

            if (Event.current.control && Event.current.type == EventType.KeyDown)
            {
                KeyCode keyCode = Event.current.keyCode;
                if (keyCode is KeyCode.Equals or KeyCode.KeypadPlus)
                {
                    FontSize++;
                    Event.current.Use();
                }

                keyCode = Event.current.keyCode;
                if (keyCode is KeyCode.Minus or KeyCode.KeypadMinus)
                {
                    FontSize--;
                    Event.current.Use();
                }
            }

            EditorGUI.DropShadowLabel(textEditorRect, _style.richText ? SyntaxHighlighting(content, _validationIssue as ParseWarnings) : content, _style);
            using (new GUIBackgroundColorScope(Color.clear))
            {
                EditorGUI.BeginChangeCheck();
                GUI.SetNextControlName(_textEditorFocusName);
                string newContent = GUI.TextArea(textEditorRect, content, _styleNoText);
                if (EditorGUI.EndChangeCheck() && content != newContent)
                {
                    content = newContent;
                    _scheduleBind = true;
                    Validate();
                }
            }

            if (editor.cursorIndex < content.Length)
            {
                int fwd = content.IndexOfAny(new[] { '}', '{' }, editor.cursorIndex);
                int bwd = content.LastIndexOfAny(new[] { '}', '{' }, Math.Max(editor.cursorIndex - 1, 0));
                if (fwd != -1 && bwd != -1 && content[fwd] == '}' && content[bwd] == '{' && Scenario.CommandPattern.IsMatch(content[bwd..(fwd + 1)]))
                    _keyOnEditorCursor = content.Substring(bwd + 1, fwd - 1 - bwd);
            }

            if (_validationIssue is ParseWarnings parsing)
            {
                foreach (Issue issue in parsing.Issues)
                {
                    Rect lineRect = default;
                    lineRect.y = _styleNoText.lineHeight * issue.SourceLine + _styleNoText.lineHeight;
                    lineRect.height = 48f;
                    lineRect.width = textEditorRect.width;
                    using (new GUIColorScope(lineRect.Contains(new Vector2(0f, _lastCursorY)) ? GUI.color.WithAlpha(0.5f) : GUI.color))
                        EditorGUI.HelpBox(lineRect, issue.Text, MessageType.Warning);
                }
            }
        }

        void DrawErrorOverlay(float editorStartHeight, float width)
        {
            if (_validationIssue == null)
                return;

            if (_validationIssue is ParseWarnings)
                return;

            string message = _validationIssue.Message;
            Rect helpBoxRect = default;
            helpBoxRect.height = 100f;
            helpBoxRect.x = width / 3 * 2;
            helpBoxRect.width = width / 3;
            helpBoxRect.y = editorStartHeight;


            GUILayout.BeginArea(helpBoxRect);
            using (new GUIColorScope(GUI.color))
                EditorGUILayout.HelpBox(message, MessageType.Error);
            GUILayout.EndArea();
        }

        void DrawBindingsOverlay(SerializedObject obj, Scenario script, Rect textEditorRect)
        {
            bool hasCursorOnBinding = !string.IsNullOrWhiteSpace(_keyOnEditorCursor);
            SerializedProperty bindings = obj.FindProperty("Bindings");
            SerializedProperty matchingProp = null;
            bool realMatch = false;
            foreach (SerializedProperty binding in bindings)
            {
                SerializedProperty name = binding.FindPropertyRelative("Name");
                matchingProp = binding;
                if (name.stringValue.Equals(_keyOnEditorCursor, StringComparison.Ordinal))
                {
                    realMatch = true;
                    break;
                }
            }

            Rect rect = textEditorRect;
            rect.height = matchingProp == null ? EditorGUIUtility.singleLineHeight : EditorGUI.GetPropertyHeight(matchingProp);
            rect.y += _lastCursorY + _style.lineHeight;
            if (!hasCursorOnBinding)
                rect.x += Screen.width * 2;
            rect.height += 30f;
            EditorGUI.DrawRect(rect.WithMargin((20f, 4f)), Color.black.WithAlpha(0.5f));
            EditorGUI.DrawRect(rect.WithMargin((22f, 6f)), Color.Lerp(Color.black, Color.white, 0.25f));
            rect = rect.WithMargin((22f, 15f));
            Rect propFieldRect = rect;
            Rect buttonRect = rect;
            if (realMatch)
                buttonRect.x += Screen.width * 2;
            else
                propFieldRect.x += Screen.width * 2;
            if (matchingProp != null)
            {
                SerializedProperty commandProp = matchingProp.FindPropertyRelative("Command");
                SerializedProperty implementOnComponentProp = matchingProp.FindPropertyRelative("IsSceneBound");
                propFieldRect.SplitWithRightOf(EditorGUIUtility.singleLineHeight, out Rect propRect, out Rect toggleRect);
                if (implementOnComponentProp.boolValue)
                    EditorGUI.SelectableLabel(propRect, "Scene-Side Implementation", EditorStyles.centeredGreyMiniLabel);
                else
                    EditorGUI.PropertyField(propRect, commandProp, GUIContent.none);
                EditorGUI.LabelField(toggleRect, new GUIContent { tooltip = ComponentTooltip });
                implementOnComponentProp.boolValue = EditorGUI.Toggle(toggleRect, implementOnComponentProp.boolValue);
            }

            if (GUI.Button(buttonRect, "Apply") && !realMatch)
            {
                script.Bindings = script.Bindings.Append(new Scenario.Binding
                {
                    Name = _keyOnEditorCursor
                }).ToArray();
            }
        }

        void DrawBindingsPanel(Scenario script, SerializedObject property)
        {
            if (!_showBindingsPanel)
                return;
            SerializedProperty bindings = property.FindProperty("Bindings");
            _bindingsScroll = EditorGUILayout.BeginScrollView(_bindingsScroll);
            Span<float> heights = stackalloc float[bindings.arraySize];
            float totalHeight = 0f;
            for (int i = 0; i < bindings.arraySize; i++)
            {
                heights[i] = EditorGUI.GetPropertyHeight(bindings.GetArrayElementAtIndex(i));
                totalHeight += heights[i];
            }

            Rect r = EditorGUILayout.GetControlRect(false, totalHeight);
            r.width -= 8f;
            r.x += 8f;
            int? toDuplicate = null;
            for (int j = 0; j < bindings.arraySize; j++)
            {
                r.height = heights[j];
                Rect propRect = r;
                propRect.width -= EditorGUIUtility.singleLineHeight * 2f;
                propRect.SplitWithRightOf(EditorGUIUtility.singleLineHeight * 3f, out propRect, out Rect buttonsRect);
                buttonsRect.SplitWithRightOf(EditorGUIUtility.singleLineHeight, out buttonsRect, out Rect deleteRect);
                buttonsRect.SplitWithRightOf(EditorGUIUtility.singleLineHeight, out Rect implementOnCompRect, out Rect duplicateRect);
                float height = implementOnCompRect.height = EditorGUIUtility.singleLineHeight;
                duplicateRect.height = height;
                r.y += r.height;
                SerializedProperty prop = bindings.GetArrayElementAtIndex(j);
                SerializedProperty implementOnComponent = prop.FindPropertyRelative("IsSceneBound");
                if (implementOnComponent.boolValue)
                {
                    propRect.Split(out Rect left, out Rect right);
                    bool wasEnabled = GUI.enabled;
                    GUI.enabled = false;
                    EditorGUI.TextField(left, prop.FindPropertyRelative("Name").stringValue);
                    EditorGUI.LabelField(right, "Scene-Side Implementation", EditorStyles.centeredGreyMiniLabel);
                    GUI.enabled = wasEnabled;
                }
                else
                    EditorGUI.PropertyField(propRect, prop);

                using (new GUIColorScope(Color.yellow))
                {
                    EditorGUI.LabelField(implementOnCompRect, new GUIContent { tooltip = ComponentTooltip });
                    implementOnComponent.boolValue = EditorGUI.Toggle(implementOnCompRect, implementOnComponent.boolValue);
                }

                using (new GUIColorScope(Color.green))
                {
                    if (GUI.Button(duplicateRect, "+"))
                        toDuplicate = j;
                }

                using (new GUIColorScope(Color.red))
                {
                    if (GUI.Button(deleteRect, "x"))
                        bindings.DeleteArrayElementAtIndex(j);
                }
            }

            if (toDuplicate.HasValue)
            {
                Scenario.Binding binding2 = script.Bindings[toDuplicate.Value];
                using (MemoryStream stream = new())
                {
                    BinaryFormatter formatter = new();
                    formatter.Serialize(stream, binding2);
                    stream.Position = 0L;
                    binding2 = (Scenario.Binding)formatter.Deserialize(stream);
                }

                var newBindings = new Scenario.Binding[script.Bindings.Length + 1];
                Array.Copy(script.Bindings, newBindings, script.Bindings.Length);
                newBindings[^1] = binding2;
                script.Bindings = newBindings;
            }

            EditorGUILayout.EndScrollView();
            if (!_scheduleBind || GUI.GetNameOfFocusedControl() == _textEditorFocusName)
                return;

            _scheduleBind = false;
            string content = script.Content;
            var previousValues = script.Bindings.ToList();
            var keys = new HashSet<string>();
            var output = new List<Scenario.Binding>();
            foreach (Match match in Scenario.CommandPattern.Matches(content))
            {
                string key = match.Groups[1].Value;
                if (keys.Add(key))
                {
                    int existingIndex = previousValues.FindIndex(x => x.Name == key);
                    if (existingIndex != -1)
                    {
                        ICommand command = previousValues[existingIndex].Command;
                        output.Add(new Scenario.Binding
                        {
                            Name = key,
                            Command = command
                        });
                        previousValues.RemoveAt(existingIndex);
                    }
                    else
                    {
                        output.Add(new Scenario.Binding
                        {
                            Name = key
                        });
                    }
                }
            }

            foreach (Scenario.Binding binding in previousValues)
            {
                if (binding.Command != null)
                {
                    output.Add(new Scenario.Binding
                    {
                        Name = binding.Name,
                        Command = binding.Command
                    });
                }
            }

            script.Bindings = output.ToArray();
        }

        class ParseWarnings : Exception
        {
            public readonly List<Issue> Issues = new();
            public override string Message => string.Join("\n", Issues);

            public void Append(Issue issue)
            {
                Issues.Add(issue);
            }
        }
    }
}