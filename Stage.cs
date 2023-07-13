using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Screech;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using static System.Globalization.UnicodeCategory;

namespace Screenplay
{
    /// <summary>
    /// Executes a given <see cref="Scenario"/>, presenting text on screen,
    /// handling player input and <see cref="ICommand"/> execution
    /// </summary>
    public class Stage
    {
        public static Action<Stage> OnOpen, OnClose, WhileOpen;

        /// <summary>
        /// Runs right before a line is shown on screen, gives you the ability to do a
        /// last minute change of its content before playing it and its functions
        /// </summary>
        public static Func<Stage, CompoundString, CompoundString> PreProcessLine;

        /// <summary>
        /// Gives you the ability to provide an arbitrary text field to output the text into
        /// </summary>
        /// <remarks>
        /// Look at <see cref="DefaultTextHandler"/> for an example implementation
        /// </remarks>
        public static Func<Stage, TMP_Text> UIGetTextFeed;

        /// <summary>
        /// Gives you the ability to provide arbitrary text fields for the choices,
        /// you will have to provide a task that you will mark as complete whenever the user choose one of the options,
        /// see <see cref="TaskCompletionSource{TResult}"/> for the implementation
        /// </summary>
        /// <remarks>
        /// Look at <see cref="DefaultChoiceHandler"/> for an example implementation
        /// </remarks>
        public static UIHandlerForChoice UIChoiceHandler;

        /// <summary>
        /// Provides control over character playback in the text field,
        /// yielding a number will display characters up to that index,
        /// yield break when the user fast forward the text or when you want to go to the next line of dialog
        /// </summary>
        /// <remarks>
        /// Look at <see cref="DefaultTypewriter"/> for an example implementation
        /// </remarks>
        public static Func<Stage, TMP_Text, IEnumerator<int>> Typewriter;

        static readonly Dictionary<Scenario, Stage> RunningStages = new();


        /// <summary>
        /// The component provided to this object's constructor, most likely a <see cref="ScenarioInScene"/>
        /// </summary>
        public readonly MonoBehaviour Component;
        /// <summary>
        /// All interlocutors currently part of this <see cref="Scenario"/>
        /// </summary>
        public readonly HashSet<IInterlocutor> Interlocutors = new();
        public readonly Scenario Scenario;

        /// <summary>
        /// An object controlled entirely by you, feel free to set it to anything you may need.
        /// Like a reference to the player object, or to the character that started this stage,
        /// or anything else that you may need when working with stages,
        /// then create an extension method to get() it in the right type so that you don't have to cast it on ever usage.
        /// </summary>
        public object Context;

        /// <summary>
        /// The interlocutor that is currently talking.
        /// </summary>
        public IInterlocutor Focus;

        /// <summary>
        /// The text field which is currently active, behaviour is undefined when retrieved outside of <see cref="ICommand.Run(Stage)"/>.
        /// </summary>
        public TMP_Text ActiveFeed;
        /// <summary>
        /// The character right after where this <see cref="ICommand"/> is bound to,
        /// behaviour is undefined when retrieved outside of <see cref="ICommand.Run(Stage)"/>.
        /// </summary>
        public int CharacterIndex;

        readonly BindingOverride[] _overrides;
        readonly IEnumerator _readingEnum;
        readonly Coroutine _unityCoroutine;

        Canvas _defaultCanvas;
        TextMeshProUGUI _defaultTextFeed;
        Image _defaultTextFeedBackground;

        /// <summary>
        /// Is this <see cref="Stage"/> done playing
        /// </summary>
        public bool IsClosed { get; private set; }

        static Stage()
        {
            #if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += delegate
            {
                var list = new List<Stage>(RunningStages.Values);
                foreach (Stage current in list)
                    current.CloseAndFree();
            };
            #endif
        }

        public Stage(ScenarioInScene component, object context = null) : this(component, component.Scenario, component.Overrides, context) { }

        Stage(MonoBehaviour component, Scenario file, BindingOverride[] overrides, object context)
        {
            Context = context;
            Component = component;
            Scenario = file;
            _overrides = overrides;
            RunningStages.Add(Scenario, this);
            _readingEnum = ReadingWrapper(file, overrides);
            try
            {
                _unityCoroutine = Component.StartCoroutine(_readingEnum);
                OnOpen?.Invoke(this);
            }
            catch (Exception)
            {
                CloseAndFree();
                throw;
            }
        }

        /// <summary>
        /// Forces this <see cref="Stage"/> to close,
        /// do know that doing so may leave your story in an unrecoverable state
        /// if, for example, the <see cref="Scenario"/> is written such that a
        /// variable must be set to a specific state before closing.
        /// </summary>
        public void CloseAndFree()
        {
            if (IsClosed)
                return;

            IsClosed = false;
            RunningStages.Remove(Scenario);
            ((IDisposable)_readingEnum).Dispose();
            if (_unityCoroutine != null && Component != null)
                Component.StopCoroutine(_unityCoroutine);
            if (_defaultCanvas != null)
                Object.Destroy(_defaultCanvas.gameObject);
            OnClose?.Invoke(this);
        }

        IEnumerator ReadingWrapper(Scenario scenario, BindingOverride[] overrides)
        {
            Reader reader;
            try
            {
                reader = new Reader(Script.Parse(scenario.AsFormattedString(false, overrides), Debug.LogWarning));
                foreach (IInterlocutor interlocutor in scenario.GetStaticInterlocutors(overrides))
                    Interlocutors.Add(interlocutor);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                CloseAndFree();
                yield break;
            }

            IEnumerator scriptReader = ReadScript(reader);
            while (true)
            {
                try
                {
                    if (!scriptReader.MoveNext())
                        break;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    CloseAndFree();
                    break;
                }

                Focus?.HasFocusTick(this);
                try
                {
                    WhileOpen?.Invoke(this);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                yield return scriptReader.Current;
            }

            CloseAndFree();
        }

        IEnumerator ReadScript(Reader reader)
        {
            while (reader.MoveNext())
            {
                var strings = new List<CompoundString>();
                foreach (FormattableString formattableString in reader.Current)
                {
                    var hairspace = new CompoundString(formattableString);
                    if (PreProcessLine != null)
                        hairspace = PreProcessLine(this, hairspace);
                    strings.Add(hairspace);
                }

                IEnumerable innerState = reader.IsChoice ? ChoiceState(strings, reader) : LineReadingState(strings[0]);
                foreach (object item in innerState)
                    yield return item;
            }
        }

        IEnumerable LineReadingState(CompoundString compoundString)
        {
            ActiveFeed = UIGetTextFeed == null ? DefaultTextHandler(this) : UIGetTextFeed(this);
            ActiveFeed.text = compoundString.Text;
            ActiveFeed.maxVisibleCharacters = 0;
            CharacterIndex = 0;
            int contentIndex = 0;
            bool typewriterActive;
            using IEnumerator<int> typewriter = Typewriter == null ? DefaultTypewriter(this, ActiveFeed) : Typewriter(this, ActiveFeed);
            do
            {
                typewriterActive = typewriter.MoveNext();
                int nextChar = typewriterActive ? typewriter.Current : ActiveFeed.text.Length - 1;
                if (nextChar > 0 && CharacterIndex == 0 && string.IsNullOrWhiteSpace(ActiveFeed.text))
                    ActiveFeed.gameObject.SetActive(true); // Only show text box when at least one character will be visible

                while (CharacterIndex < nextChar)
                {
                    if (ActiveFeed.text[CharacterIndex++] == CompoundString.ContentMarker)
                    {
                        foreach (object item in ((ICommand)compoundString.Contents[contentIndex++].Object).Run(this))
                            yield return item;
                    }

                    ActiveFeed.maxVisibleCharacters = CharacterIndex;
                }

                ActiveFeed.maxVisibleCharacters = nextChar;
                yield return null;
            } while (typewriterActive);
        }

        IEnumerable ChoiceState(List<CompoundString> branches, Reader reader)
        {
            TMP_Text[] choiceTextComp;
            Task<TMP_Text> choosing;
            if (UIChoiceHandler == null)
                DefaultChoiceHandler(this, branches.Count, out choiceTextComp, out choosing);
            else
                UIChoiceHandler(this, branches.Count, out choiceTextComp, out choosing);

            for (int i = 0; i < branches.Count; i++)
            {
                CompoundString compoundString = branches[i];
                ActiveFeed = choiceTextComp[i];
                ActiveFeed.text = compoundString.Text;
                CharacterIndex = 0;
                int cont = 0;
                while (CharacterIndex < ActiveFeed.text.Length)
                {
                    if (ActiveFeed.text[CharacterIndex++] != CompoundString.ContentMarker)
                        continue;
                    foreach (object item in ((ICommand)compoundString.Contents[cont++].Object).Run(this))
                        yield return item;
                }
            }

            while (!choosing.IsCompleted)
                yield return null;

            reader.Choose(Array.IndexOf(choiceTextComp, choosing.Result));
        }

        /// <summary>
        /// Creates a new stage using this one's component and context as a basis, used by <see cref="Commands.SwapScenario"/>
        /// </summary>
        public static Stage NewStageFrom(Stage stage, Scenario scenario, BindingOverride[] overrides = null) => new(stage.Component, scenario, overrides ?? stage._overrides, stage.Context);

        /// <summary>
        /// Whether the given <paramref name="tree"/> is currently running, if so returns the <paramref name="stage"/> it runs through
        /// </summary>
        public static bool IsRunning(Scenario tree, out Stage stage) => RunningStages.TryGetValue(tree, out stage);

        /// <summary>
        /// Returns all existing and running <see cref="Scenario"/>s
        /// </summary>
        public static Dictionary<Scenario, Stage>.Enumerator RunningScenarios() => RunningStages.GetEnumerator();

        /// <summary>
        /// Returns the first <see cref="Stage"/> this <see cref="IInterlocutor"/> is participating in if any
        /// </summary>
        public static bool IsParticipating(IInterlocutor chara, out Stage foundStage)
        {
            foreach ((Scenario script, Stage instance) in RunningStages)
            {
                if (instance.Interlocutors.Contains(chara))
                {
                    foundStage = instance;
                    return true;
                }
            }

            foundStage = null;
            return false;
        }

        static TMP_Text DefaultTextHandler(Stage stage)
        {
            Canvas canvas = GetDefaultCanvas(stage);
            ref TextMeshProUGUI textFeed = ref stage._defaultTextFeed;
            ref Image background = ref stage._defaultTextFeedBackground;

            if (textFeed == null)
            {
                const float bg_color = 41f / 255f;
                const float deco_color = 34f / 255f;

                background = new GameObject("TextFeed").AddComponent<Image>();
                background.transform.SetParent(canvas.transform, false);
                background.color = new Color(bg_color, bg_color, bg_color);
                background.sprite = Resources.Load<Sprite>("UI/Skin/InputFieldBackground.psd");

                RectTransform tr = (RectTransform)background.transform;
                tr.anchorMin = default;
                tr.anchorMax = new Vector2(1f, 0.25f);
                tr.offsetMin = new Vector2(10f, 10f);
                tr.offsetMax = new Vector2(-10f, -10f);

                Image decoration1 = new GameObject("Decoration").AddComponent<Image>();
                Image decoration2 = new GameObject("Decoration").AddComponent<Image>();
                decoration2.color = decoration1.color = new Color(deco_color, deco_color, deco_color);
                tr = (RectTransform)decoration1.transform;
                tr.anchorMin = new Vector2(0f, 0f);
                tr.anchorMax = new Vector2(1f, 0f);
                tr.offsetMin = new Vector2(5f, 5f);
                tr.offsetMax = new Vector2(-5f, 10f);

                tr = (RectTransform)decoration2.transform;
                tr.anchorMin = new Vector2(0f, 1f);
                tr.anchorMax = new Vector2(1f, 1f);
                tr.offsetMin = new Vector2(5f, -10f);
                tr.offsetMax = new Vector2(-5f, -5f);

                decoration1.transform.SetParent(background.transform, false);
                decoration2.transform.SetParent(background.transform, false);

                GameObject go = new("TextFeed");
                go.AddComponent<CanvasRenderer>();
                textFeed = go.AddComponent<TextMeshProUGUI>();
                textFeed.transform.SetParent(background.transform, false);
                textFeed.alignment = TextAlignmentOptions.TopLeft;
                textFeed.overflowMode = TextOverflowModes.ScrollRect;
                textFeed.fontStyle = FontStyles.Bold | FontStyles.SmallCaps;

                tr = (RectTransform)textFeed.transform;
                tr.anchorMin = default;
                tr.anchorMax = Vector2.one;
                tr.offsetMin = new Vector2(40f, 40f);
                tr.offsetMax = new Vector2(-40f, -40f);
            }

            return stage._defaultTextFeed;
        }

        static Canvas GetDefaultCanvas(Stage stage)
        {
            ref Canvas canvas = ref stage._defaultCanvas;
            if (canvas == null)
            {
                canvas = new GameObject("Temporary Canvas").AddComponent<Canvas>();
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                canvas.gameObject.AddComponent<CanvasScaler>();
                if (Object.FindObjectOfType<EventSystem>() is null)
                {
                    EventSystem eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
                    eventSystem.gameObject.AddComponent<StandaloneInputModule>();
                    eventSystem.gameObject.AddComponent<BaseInput>();
                }

                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            return canvas;
        }

        static void DefaultChoiceHandler(Stage stage, int choices, out TMP_Text[] choiceTextComp, out Task<TMP_Text> choosing)
        {
            choiceTextComp = new TMP_Text[choices];
            var tcs = new TaskCompletionSource<TMP_Text>();
            choosing = tcs.Task;
            var buttons = new (RectTransform, TextMeshProUGUI)[choices];
            Canvas canvas = GetDefaultCanvas(stage);
            for (int i = 0; i < buttons.Length; i++)
            {
                GameObject goButton = new("Button");
                Image image = goButton.AddComponent<Image>();
                image.color = new Color(41f/255f, 41f/255f, 41f/255f);
                Button button = goButton.AddComponent<Button>();
                button.targetGraphic = image;
                TextMeshProUGUI text = new GameObject("Text").AddComponent<TextMeshProUGUI>();
                text.alignment = TextAlignmentOptions.Center;
                goButton.transform.SetParent(canvas.transform, false);
                text.transform.SetParent(canvas.transform, false);
                text.fontStyle = FontStyles.Bold | FontStyles.SmallCaps;

                choiceTextComp[i] = text;
                text.raycastTarget = false; // blocks buttons otherwise

                RectTransform textTransform = (RectTransform)text.transform;
                textTransform.anchorMin = default;
                textTransform.anchorMax = Vector2.one;
                textTransform.anchoredPosition3D = default;
                button.onClick.AddListener(delegate
                {
                    tcs.SetResult(text);
                    foreach (var (button, text) in buttons)
                    {
                        Object.Destroy(button.gameObject);
                        Object.Destroy(text.gameObject);
                    }
                });
                buttons[i] = ((RectTransform)button.transform, text);
                if (i == 0)
                    button.StartCoroutine(FitButtonRect());
            }

            IEnumerator FitButtonRect()
            {
                while (true)
                {
                    float y = 0f;
                    foreach (var (button, text) in buttons)
                    {
                        if(text == null)
                            continue;

                        text.rectTransform.anchoredPosition = new Vector2(0f, y);
                        button.anchoredPosition = new Vector2(0f, y);
                        Vector2 vec2 = text.GetPreferredValues();
                        button.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, vec2.x + 40f);
                        button.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, vec2.y + 20f);
                        y -= vec2.y + 30f;
                    }

                    yield return null;
                }
            }
        }

        static IEnumerator<int> DefaultTypewriter(Stage stage, TMP_Text comp)
        {
            float delay = 0f;
            for (int i = 0; i < comp.text.Length; i++)
            {
                char c = comp.text[i];
                if (c == '<' && comp.text.IndexOf('>', i) is int closing && closing != -1) // Skip html tags
                {
                    i = closing;
                    continue;
                }

                delay += char.GetUnicodeCategory(c) switch
                {
                    OpenPunctuation or ConnectorPunctuation or ClosePunctuation => 0f,
                    InitialQuotePunctuation or FinalQuotePunctuation => 0f,
                    DashPunctuation => 0f,
                    OtherPunctuation => c switch
                    {
                        '\'' or '\"' => 0f,
                        ',' or ';' => 0.5f,
                        _ => 2f
                    },
                    TitlecaseLetter or UppercaseLetter or LowercaseLetter => 0.1f,
                    ModifierLetter or OtherLetter => 0f,
                    DecimalDigitNumber or LetterNumber or OtherNumber => 2f,
                    MathSymbol or CurrencySymbol or OtherSymbol => 2f,
                    ModifierSymbol => 0f,
                    Format or Control => 0f,
                    EnclosingMark or NonSpacingMark => 0f,
                    SpacingCombiningMark or SpaceSeparator or LineSeparator or ParagraphSeparator => 0f,
                    OtherNotAssigned or PrivateUse or Surrogate or _ => 0f,
                };

                for (; delay > 0f; delay -= Time.deltaTime * 3f)
                {
                    if (FastForward())
                    {
                        delay = 0f; // Break through this loop
                        i = comp.text.Length - 1; // Break through outer loop
                    }

                    yield return i + 1; // Hold same character while going through the delay
                }
            }

            if (string.IsNullOrWhiteSpace(comp.text)) // Start next line right away for lines with only logic
                yield break;

            while (FastForward() == false) // Waiting for user input before starting next line
                yield return comp.text.Length;

            static bool FastForward() => Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
        }
    }
}