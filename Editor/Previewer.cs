using System;
using System.Collections.Generic;
using System.Threading;
using Screenplay.Component;
using UnityEditor;
using UnityEngine;
using Screenplay.Nodes;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Screenplay.Editor
{
    public class Previewer : IPreviewer, IDisposable
    {
        private CancellationTokenSource _cancellationTokenSource = new();
        private int _running;
        private List<Func<CancellationToken, Awaitable>> _asynchronousRunner = new();
        private Stack<Action> _rollbacksRegistered = new();
        private bool _loopPreview;
        private UIBase? _dialogUIComponentPrefab, _dialogUI;

        public ScreenplayGraph Source { get; }

        public List<IScreenplayNode> Path { get; }

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

        public Previewer(bool loopPreview, UIBase? dialogUIComponentPrefab, List<IScreenplayNode> path, ScreenplayGraph sourceParam)
        {
            _loopPreview = loopPreview;
            _dialogUIComponentPrefab = dialogUIComponentPrefab;
            EditorApplication.update += Update;
            Source = sourceParam;
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnInspectorDrawHeaderGUI;
            Path = path;
        }

        public void Dispose()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI -= OnInspectorDrawHeaderGUI;

            try
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

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
            if (obj.target is GameObject or UnityEngine.Component)
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
                if (_loopPreview && _running == 0)
                {
                    foreach (var func in _asynchronousRunner)
                        RunAndMonitorExit(func);
                }

                if (_running > 0)
                {
                    //SceneView.RepaintAll();
                    EditorApplication.QueuePlayerLoopUpdate();
                }
            }
        }

        private async void RunAndMonitorExit(Func<CancellationToken, Awaitable> runner)
        {
            try
            {
                _running++;
                await runner(_cancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                if (e is not OperationCanceledException)
                    Debug.LogException(e);
            }
            finally
            {
                _running--;
            }
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

        public void RegisterRollback(Animator animator, int hash, int layer)
        {
            var animState = new AnimationRollback(animator, hash, layer);
            _rollbacksRegistered.Push(() => { animState.Rollback(); });
        }

        public void AddCustomPreview(Func<CancellationToken, Awaitable> signal)
        {
            RunAndMonitorExit(signal);
            _asynchronousRunner.Add(signal);
        }

        public bool Visited(IPrerequisite executable) => false;

        public void Visiting(IBranch? executable) { }
    }
}
