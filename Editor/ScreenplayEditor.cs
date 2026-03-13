using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Screenplay.Component;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using YNode;
using YNode.Editor;
using Screenplay.Nodes;
using UnityEngine.Serialization;
using Event = Screenplay.Nodes.Event;
using Random = Unity.Mathematics.Random;

namespace Screenplay.Editor
{
    public class ScreenplayEditor : CustomGraphWindow<ScreenplayGraph>
    {
        private List<IScreenplayNode> _previewChain = new();
        private List<IScreenplayNode> _rootToPreview = new();
        private HashSet<IScreenplayNode> _reachable = new();
        private Action? _disposeCallbacks;
        private Previewer? _previewer;
        private bool _previewEnabled, _mapEnabled = true, _quickjump = true;
        private PreviewFlags _previewFlags = PreviewFlags.Loop | PreviewFlags.SelectedChain;
        private bool _hasFocus;
        private Vector2 _quickjumpScroll;
        private uint _fixedSeed;
        private Random _random = new Random(1);
        private List<ScreenplayGraph.EventProgress> _saveRestoreResult = new();

        public bool InPreviewChain(IScreenplayNode node) => _previewChain.Contains(node);
        public bool IsReachable(IScreenplayNode node) => _reachable.Contains(node);

        protected override bool StickyEditorEnabled => false;
        protected override Rect NodeMap => _mapEnabled ? base.NodeMap : default;

        protected override void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            base.OnGUI();

            if (EditorGUI.EndChangeCheck())
            {
                Rollback();
                TryPreview();
                RecalculateReachable();
            }
        }

        protected override void Load()
        {
            base.Load();
            RecalculateReachable();
        }

        protected override void OnGUIOverlay()
        {
            base.OnGUIOverlay();

            if (_saveRestoreResult.Count > 0)
            {
                var noodleThickness = Preferences.GetSettings().NoodleThickness;
                var pts = new List<Vector2>();
                foreach (var eventProgress in _saveRestoreResult)
                {
                    Color startColor = Color.yellow;
                    Color endColor = Color.green;
                    for (int i = 0; i < eventProgress.ExecutionOrder.Count; i++)
                    {
                        ScreenplayGraph.Link link = eventProgress.ExecutionOrder[i];
                        if (NodesToEditor.TryGetValue(link.Previous, out var prev) == false)
                            continue;
                        if (NodesToEditor.TryGetValue(link.Next, out var next) == false)
                            continue;

                        var start = prev.Value.Position + prev.CachedSize * new Vector2(1f, 0.5f);
                        var end = next.Value.Position + next.CachedSize * new Vector2(0f, 0.5f);
                        pts.Add(start);
                        pts.Add(end);

                        var color = Color.Lerp(startColor, endColor, i / (float)eventProgress.ExecutionOrder.Count);

                        var previousColor = GUI.color;
                        GUI.color = color;
                        GUI.Label(new Rect(GridToWindowPosition(start * 0.5f + end * 0.5f), new Vector2(100, 16)), i.ToString());
                        GUI.color = previousColor;

                        DrawNoodle((color, color), NoodlePath.Angled, NoodleStroke.Dashed, noodleThickness, pts);
                        DrawArrow(IO.Output, WindowToGridPosition(pts[1]), color);
                        pts.Clear();
                    }
                }
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                _quickjump = GUILayout.Toggle(_quickjump, "Quickjump table", EditorStyles.toolbarButton);

                _fixedSeed = (uint)EditorGUILayout.IntField(new GUIContent("Seed:"), (int)_fixedSeed);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Preview Save Restore", EditorStyles.toolbarButton))
                {
                    SelectionToSaveRestorePreview();
                }

                Graph.DebugRetainProgressInEditor = GUILayout.Toggle(Graph.DebugRetainProgressInEditor, "Retain Progress", EditorStyles.toolbarButton);

                _mapEnabled = GUILayout.Toggle(_mapEnabled, "Map", EditorStyles.toolbarButton);

                _previewEnabled = GUILayout.Toggle(_previewEnabled, "Preview", EditorStyles.toolbarButton);

                _previewFlags = (PreviewFlags)EditorGUILayout.EnumFlagsField(_previewFlags, EditorStyles.toolbarPopup);
            }
            EditorGUILayout.EndHorizontal();

            if (_quickjump)
            {
                var arr = ArrayPool<Event>.Shared.Rent(Graph.Nodes.Count);
                var arrSpan = arr.AsSpan()[..0];
                foreach (var node in Graph.Nodes)
                {
                    if (node is Event e)
                    {
                        arrSpan = arr.AsSpan()[..(arrSpan.Length + 1)];
                        arrSpan[^1] = e;
                    }
                }
                Array.Sort(arr, 0, arrSpan.Length, SortByXPos.Instance);

                _quickjumpScroll = EditorGUILayout.BeginScrollView(_quickjumpScroll, GUILayout.Width(150));
                EditorGUILayout.BeginVertical(GUILayout.MaxWidth(150));
                foreach (var e in arrSpan)
                {
                    var r = GUILayoutUtility.GetRect(GUIContent.none, ButtonWithClipping);
                    if (GUI.Button(r, e.Name, ButtonWithClipping))
                    {
                        Vector2 nodeDimension = NodesToEditor[e].CachedSize / 2;
                        PanOffset = -e.Position - nodeDimension;
                    }
                }
                EditorGUILayout.EndVertical();
                ArrayPool<Event>.Shared.Return(arr);
                EditorGUILayout.EndScrollView();
            }

            TryPreview();

            var dispatcher = FindFirstObjectByType<ScreenplayDispatcher>();
            if (dispatcher == null)
            {
                if (GUILayout.Button(EditorGUIUtility.TrTextContentWithIcon($"You must add a {nameof(ScreenplayDispatcher)} to the scene to run this screenplay,\nclick me to add one automatically", MessageType.Warning), EditorStyles.helpBox))
                {
                    var go = new GameObject(nameof(ScreenplayDispatcher));
                    go.AddComponent<ScreenplayDispatcher>().Screenplay = Graph;
                    EditorUtility.SetDirty(go);
                }
            }
            else if (dispatcher.Screenplay == null)
            {
                if (GUILayout.Button(EditorGUIUtility.TrTextContentWithIcon($"The existing {nameof(ScreenplayDispatcher)} does not have any screenplay set to it,\nclick me to set it automatically", MessageType.Warning), EditorStyles.helpBox))
                    dispatcher.Screenplay = Graph;
            }
            else if (dispatcher.Screenplay != Graph)
            {
                EditorGUILayout.HelpBox($"The existing {nameof(ScreenplayDispatcher)} doesn't run with this screenplay, you might want to assign it to this screenplay", MessageType.Warning);
            }
        }

        private void SelectionToSaveRestorePreview()
        {
            var selection = Selection.objects.Select(x => x as NodeEditor).NotNull().ToList();
            var @event = selection.FirstOrDefault(x => x.Value is Event)?.Value;
            if (@event is null)
            {
                _saveRestoreResult = new();
                Debug.LogError("Select at least one event");
                return;
            }

            var executables = selection.Select(x => x.Value as IExecutable).NotNull().ToList();
            var serialized = new List<ScreenplayGraph.ExecutableSerialized>();
            for (int i = 1; i < executables.Count; i++)
            {
                serialized.Add(new()
                {
                    Previous = ManagedReferenceUtility.GetManagedReferenceIdForObject(Graph, executables[i-1]),
                    Next = ManagedReferenceUtility.GetManagedReferenceIdForObject(Graph, executables[i])
                });
            }
            var state = new ScreenplayGraph.State
            {
                Events = new()
                {
                    new ScreenplayGraph.State.EventPlayback
                    {
                        Local = Array.Empty<GlobalId>(),
                        EventId = ManagedReferenceUtility.GetManagedReferenceIdForObject(Graph, @event),
                        Executables = serialized,
                        FirstExecutable = ManagedReferenceUtility.GetManagedReferenceIdForObject(Graph, executables.Count > 0 ? executables[0] : ((Event)@event).Action)
                    }
                }
            };
            _saveRestoreResult = ScreenplayGraph.State.Reconstruct(state, ScreenplayGraph.RestoreBehavior.NoRestriction, out ScreenplayGraph.RestoreBehavior effects, Graph);
            Debug.LogWarning(effects);
        }

        private void RecalculateReachable()
        {
            _reachable.Clear();

            foreach (var node in Graph.Nodes)
            {
                if (node is ICustomEntry or Event)
                    TraverseTree(node, _reachable);
            }

            static void TraverseTree(INodeValue? node, HashSet<IScreenplayNode> reachable)
            {
                if (node is not IScreenplayNode screenplayNode)
                    return;

                if (reachable.Add(screenplayNode) == false)
                    return;

                if (screenplayNode is IBranch b)
                {
                    foreach (var otherActions in b.Followup())
                        TraverseTree(otherActions, reachable);
                }
            }
        }

        private void TryPreview()
        {
            var previousSelection = _previewChain.Count > 0 ? _previewChain[^1] : null;
            _previewChain.Clear();
            if (_previewEnabled
                && (_previewFlags.Contains(PreviewFlags.Unfocused) || _hasFocus)
                && (_previewFlags.Contains(PreviewFlags.InPlayMode) || EditorApplication.isPlaying == false))
            {
                if (Selection.activeObject is NodeEditor selectedNode && selectedNode.Graph == Graph && selectedNode.Value is IPreviewable selectedPreviewable)
                {
                    _rootToPreview.Clear();
                    if (selectedPreviewable is IBranch selectedAction)
                        Graph.IsNodeReachable(selectedAction, _rootToPreview);
                    if (_previewFlags.Contains(PreviewFlags.SelectedChain) && selectedPreviewable is IBranch)
                        _previewChain.AddRange(_rootToPreview);
                    else
                        _previewChain.Add(selectedPreviewable);
                }
            }

            var currentSelection = _previewChain.Count > 0 ? _previewChain[^1] : null;
            if (currentSelection is null)
            {
                Rollback();
            }
            else if (_previewer is null || previousSelection != currentSelection) // Selection changed
            {
                Rollback();
                _previewer = new Previewer(_previewFlags.Contains(PreviewFlags.Loop), _fixedSeed == 0 ? _random.NextUInt(1, uint.MaxValue) : _fixedSeed, _rootToPreview, new ScreenplayGraph.Introspection{ Graph = Graph });
                for (int i = 0; i < _previewChain.Count; i++)
                {
                    if (_previewChain[i] is IPreviewable previewable)
                        previewable.SetupPreview(_previewer, i+1 != _previewChain.Count);
                }
            }
        }

        private static readonly List<Vector3> _positions = new();
        private static readonly HashSet<NodeEditor> _traversed = new();
        private static readonly ReferenceCollector _collector = new();

        private void OnSceneGUI(SceneView view)
        {
            bool rebuildPreview = false;
            var proxy = SceneGUIProxy.Instance;
            proxy.BeginChangeCheck();
            foreach (var o in Selection.objects)
            {
                if (o is NodeEditor nodeEditor && nodeEditor.Value is INodeWithSceneGizmos sceneGizmos)
                {
                    using (proxy.AutoUndo(Graph, sceneGizmos.GetType().Name))
                    {
                        sceneGizmos.DrawGizmos(proxy, Graph, ref rebuildPreview);
                    }
                }
            }

            if (proxy.EndChangeCheck() || rebuildPreview)
            {
                Rollback();
                TryPreview();
            }

            SceneGUIProxy gui = SceneGUIProxy.Instance;
            foreach (var node in Graph.Nodes)
            {
                if (node is Event or ICustomEntry && NodesToEditor.TryGetValue(node, out var eventEditor))
                {
                    CollectConnectedEditors(eventEditor);

                    foreach (var nodeEditor in _traversed)
                    {
                        if (nodeEditor.Value is not IScreenplayNode sp)
                            continue;

                        sp.CollectReferences(_collector);
                        for (int j = _collector.RawData.Count - 1; j >= 0; j--)
                        {
                            var genericSceneObjectReference = _collector.RawData[j];
                            if (genericSceneObjectReference.TryGet(out var obj, out _))
                            {
                                Vector3 otherPos;
                                if (obj is GameObject go)
                                    otherPos = go.transform.position;
                                else if (obj is UnityEngine.Component c)
                                    otherPos = c.transform.position;
                                else
                                    continue;

                                _positions.Add(otherPos);
                                gui.Label(otherPos, nodeEditor.Value.GetType().Name);
                            }
                        }

                        _collector.Clear();
                    }

                    Vector3 middle = default;
                    foreach (var vector3 in _positions)
                        middle += vector3 / _positions.Count;

                    if (_positions.Count > 0)
                    {
                        if (node is Event e)
                            gui.Label(middle, e.Name);
                        else
                            gui.Label(middle, node.GetType().Name);

                        var handleSize = HandleUtility.GetHandleSize(middle) * 0.1f;
                        Handles.color = Color.blue;
                        if (Handles.Button(middle, Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap))
                        {
                            Vector2 nodeDimension = NodesToEditor[node].CachedSize / 2;
                            PanOffset = -node.Position - nodeDimension;
                        }
                    }

                    foreach (var vector3 in _positions)
                        gui.DottedLine(vector3, middle);

                    _traversed.Clear();
                    _collector.Clear();
                    _positions.Clear();
                }
            }

            static void CollectConnectedEditors(NodeEditor editor)
            {
                if (_traversed.Add(editor) == false)
                    return;

                foreach (var activePort in editor.ActivePorts)
                {
                    if (activePort.Value.ConnectedEditor is null)
                        continue;

                    CollectConnectedEditors(activePort.Value.ConnectedEditor);
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;

            AssemblyReloadEvents.AssemblyReloadCallback assReloadCallback = Rollback;
            Action<PlayModeStateChange> pmsChanged = change => Rollback();
            EditorSceneManager.SceneSavingCallback ssCallback = (scene, path) => Rollback();
            EditorSceneManager.SceneClosingCallback scCallback = (scene, path) => Rollback();

            AssemblyReloadEvents.beforeAssemblyReload += assReloadCallback;
            EditorApplication.playModeStateChanged += pmsChanged;
            EditorSceneManager.sceneSaving += ssCallback;
            EditorSceneManager.sceneClosing += scCallback;

            _disposeCallbacks += () =>
            {
                AssemblyReloadEvents.beforeAssemblyReload -= assReloadCallback;
                EditorApplication.playModeStateChanged -= pmsChanged;
                EditorSceneManager.sceneSaving -= ssCallback;
                EditorSceneManager.sceneClosing -= scCallback;
            };
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _disposeCallbacks?.Invoke();
            _disposeCallbacks = null;
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        public override void OnFocus()
        {
            base.OnFocus();
            _hasFocus = true;
        }

        public override void OnLostFocus()
        {
            base.OnLostFocus();
            if (_previewFlags.Contains(PreviewFlags.Unfocused) == false)
                Rollback();
            _hasFocus = false;
        }

        public override string GetNodeMenuName(System.Type type)
        {
            // ReSharper disable once RedundantNameQualifier
            if (typeof(AbstractScreenplayNode).IsAssignableFrom(type) || type == typeof(Notes))
            {
                return base.GetNodeMenuName(type);
            }

            return "";
        }

        public void Rollback()
        {
            _previewer?.Dispose();
            _previewer = null;
        }

        [Flags]
        public enum PreviewFlags
        {
            [Tooltip("Preview the whole chain up to the selected node")]
            SelectedChain =  0b0001,
            [Tooltip("Restart the previewed node as soon as it finished the preview")]
            Loop =           0b0010,
            [Tooltip("Play the preview even when the window is not in focus, do know that making changes to things that are being played will lead to undefined behavior")]
            Unfocused =      0b0100,
            [Tooltip("Play the preview even when a game is currently running")]
            InPlayMode =     0b1000,
        }

        private class SortByXPos : IComparer<Event>
        {
            public static SortByXPos Instance = new SortByXPos();
            public int Compare(Event x, Event y) => x.Position.x.CompareTo(y.Position.x);
        }

        private static GUIStyle? s_buttonWithClipping;
        private static GUIStyle ButtonWithClipping => s_buttonWithClipping ??= new GUIStyle(EditorStyles.miniButton)
        {
#if UNITY_2023_1_OR_NEWER
            clipping = TextClipping.Ellipsis,
#else
            clipping = TextClipping.Clip,
#endif
        };
    }

    public static class EnumExtension
    {
        public static bool Contains(this ScreenplayEditor.PreviewFlags a, ScreenplayEditor.PreviewFlags b) =>
            (a & b) == b;
    }
}
