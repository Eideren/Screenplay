using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Screenplay;
using Screenplay.Component;
using UnityEditor;
using UnityEngine;
using Screenplay.Nodes;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Source.Screenplay.Editor
{
    public class Previewer : IPreviewer, IDisposable
    {
        private Dictionary<object, CancellationTokenSource> _asynchronousRunner = new();
        private Stack<System.Action> _rollbacksRegistered = new();
        private bool _loopPreview;
        private UIBase? _dialogUIComponentPrefab, _dialogUI;

        public ScreenplayGraph Source { get; }
        public bool Loop => _loopPreview;

        public UIBase? GetDialogUI()
        {
            if (_dialogUIComponentPrefab == null)
                return null;

            if (_dialogUI == null)
            {
                _dialogUI = Object.Instantiate(_dialogUIComponentPrefab);
                _rollbacksRegistered.Push(() => Object.DestroyImmediate(_dialogUI.gameObject));
            }

            return _dialogUI;
        }

        public HashSet<IPrerequisite> Visited { get; } = new();

        public Previewer(bool loopPreview, UIBase? dialogUIComponentPrefab, ScreenplayGraph sourceParam)
        {
            _loopPreview = loopPreview;
            _dialogUIComponentPrefab = dialogUIComponentPrefab;
            EditorApplication.update += Update;
            Source = sourceParam;
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnInspectorDrawHeaderGUI;
        }

        public void Dispose()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI -= OnInspectorDrawHeaderGUI;

            foreach (var (_, cancellation) in _asynchronousRunner.ToArray())
            {
                cancellation.Cancel();
                cancellation.Dispose();
            }

            _asynchronousRunner.Clear();

            while (_rollbacksRegistered.TryPop(out var action))
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            EditorApplication.update -= Update;
            EditorApplication.QueuePlayerLoopUpdate();
        }

        ~Previewer()
        {
            if (_rollbacksRegistered.Count > 0 || _asynchronousRunner.Count > 0)
            {
                Debug.LogError("A previewer was not correctly disposed, your scene likely has changes introduced from a preview that cannot be rolled back");
            }
        }

        private void OnInspectorDrawHeaderGUI(UnityEditor.Editor obj)
        {
            if (obj.target is GameObject or Component)
            {
                var prevColor = GUI.color;
                GUI.color = Color.yellow * new Color(1,1,1,0.1f);
                GUI.DrawTexture(new Rect(0,0, 100000,100000), Texture2D.whiteTexture);
                GUI.color = prevColor;
                EditorGUILayout.HelpBox($"A {nameof(Screenplay)} is currently being previewed, changes you introduce now may be rolled back once preview is done", MessageType.Warning);
            }
        }

        private void Update()
        {
            if (_asynchronousRunner.Count > 0)
            {
                //SceneView.RepaintAll();
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        public void RunAsynchronously(object key, Func<CancellationToken, Awaitable> runner)
        {
            StopAsynchronous(key);
            var source = new CancellationTokenSource();
            _ = RunAndDiscard(key, source.Token, runner);
            _asynchronousRunner.Add(key, source);
        }

        async Awaitable RunAndDiscard(object key, CancellationToken cancellation, Func<CancellationToken, Awaitable> runner)
        {
            try
            {
                await runner(cancellation);
            }
            catch (Exception e)
            {
                if (e is not OperationCanceledException)
                    Debug.LogException(e);
            }
            finally
            {
                _asynchronousRunner.Remove(key);
            }
        }

        public bool StopAsynchronous(object key)
        {
            if (_asynchronousRunner.Remove(key, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                return true;
            }

            return false;
        }

        public void RegisterRollback(System.Action rollback)
        {
            _rollbacksRegistered.Push(rollback);
        }

        public void RegisterRollback(AnimationClip clip, GameObject go)
        {
            var animState = new AnimationRollback(go, clip);
            _rollbacksRegistered.Push(() => { animState.Rollback(); });
        }

        public void PlayCustomSignal(Func<CancellationToken, Awaitable> signal)
        {
            RunAsynchronously(new object(), signal);
        }
    }
}
