using System;
using System.Buffers;
using System.Collections.Generic;
using Screenplay.Component;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using YNode;
using YNode.Editor;
using Screenplay.Nodes;
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

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                _quickjump = GUILayout.Toggle(_quickjump, "Quickjump table", EditorStyles.toolbarButton);

                _fixedSeed = (uint)EditorGUILayout.IntField(new GUIContent("Seed:"), (int)_fixedSeed);

                GUILayout.FlexibleSpace();

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

            if (Graph.DialogUIPrefab == null)
            {
                if (GUILayout.Button(EditorGUIUtility.TrTextContentWithIcon("This graph does not have a UI setup to handle dialogs,\nclick me to select the asset it in the inspector", MessageType.Warning), EditorStyles.helpBox))
                    Selection.objects = new[] { Graph };
            }
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
                _previewer = new Previewer(_previewFlags.Contains(PreviewFlags.Loop), _fixedSeed == 0 ? _random.NextUInt(1, uint.MaxValue) : _fixedSeed, Graph.DialogUIPrefab, _rootToPreview, new ScreenplayGraph.Introspection(){ Graph = Graph, EventsReady = new() });
                for (int i = 0; i < _previewChain.Count; i++)
                {
                    if (_previewChain[i] is IPreviewable previewable)
                        previewable.SetupPreview(_previewer, i+1 != _previewChain.Count);
                }
            }
        }

        private void OnSceneGUI(SceneView view)
        {
            bool rebuildPreview = false;
            foreach (var o in Selection.objects)
            {
                if (o is YNode.Editor.NodeEditor nodeEditor && nodeEditor.Value is INodeWithSceneGizmos sceneGizmos)
                    sceneGizmos.DrawGizmos(ref rebuildPreview);
            }

            if (rebuildPreview)
            {
                Rollback();
                TryPreview();
            }
        }

        private ScreenplayNodeEditor? TryGetEditorFromValue(INodeValue value)
        {
            return NodesToEditor[value] as ScreenplayNodeEditor;
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
