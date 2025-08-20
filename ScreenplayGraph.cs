using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using YNode;
using Screenplay.Nodes;
using Screenplay.Nodes.Triggers;
using Sirenix.OdinInspector;
using Event = Screenplay.Nodes.Event;

namespace Screenplay
{
    [CreateAssetMenu(menuName = "Screenplay/Screenplay")]
    public class ScreenplayGraph : NodeGraph, ISerializationCallbackReceiver
    {
        [Required]
        public Component.UIBase? DialogUIPrefab;
        public bool DebugRetainProgressInEditor;
        private HashSet<IPrerequisite> _visited = new();
        private HashSet<Event> _visitedEvents = new();

        // Only serialized for editor reloading purposes
        [SerializeField, HideInInspector] private IAction? _action;
        [SerializeField, HideInInspector, SerializeReference] private List<IPrerequisite> __visitedSerializationProxy = new ();
        [SerializeField, HideInInspector, SerializeReference] private List<Event> __visitedEventsSerializationProxy = new ();

        /// <summary>
        /// You must dispose of this Awaitable when reloading a running game.
        /// </summary>
        public async Awaitable StartExecution(CancellationToken cancellation)
        {
            using var context = new DefaultContext(this);
            var events = new List<Event>();
            var triggers = new Dictionary<Event, ITrigger>();
            try
            {
                foreach (var value in Nodes)
                {
                    if (value is Event e && e.Action is not null && (e.Repeatable || _visitedEvents.Contains(e) == false))
                        events.Add(e);
                    if (value is ICustomEntry customEntry)
                        customEntry.Run(context.Visited, cancellation);
                }

                do
                {
                    if (_action is null) // Check non-triggerable
                    {
                        foreach (var e in events)
                        {
                            if (e.TriggerSource is not null)
                                continue;
                            if (e.Prerequisite?.TestPrerequisite(_visited) == false)
                                continue;

                            _visitedEvents.Add(e);
                            if (e.Repeatable == false)
                                events.Remove(e);
                            _action = e.Action;
                            break;
                        }
                    }

                    if (_action is null) // Check triggerable
                    {
                        foreach (var e in events)
                        {
                            if (e.TriggerSource is null)
                                continue;

                            if (e.Prerequisite?.TestPrerequisite(_visited) == false)
                            {
                                if (triggers.TryGetValue(e, out var outdatedTrigger))
                                    outdatedTrigger.Dispose();
                                continue;
                            }

                            if (triggers.ContainsKey(e))
                                continue;

                            System.Action callback = () =>
                            {
                                if (_action is not null)
                                    return; // Another trigger already queued one

                                _visitedEvents.Add(e);
                                if (e.Repeatable == false)
                                    events.Remove(e);
                                _action = e.Action;
                            };

                            if (e.TriggerSource.TryCreateTrigger(callback, out var trigger) == false)
                                continue;

                            triggers.Add(e, trigger);
                            break;
                        }
                    }

                    if (_action is null)
                    {
                        await Awaitable.NextFrameAsync(cancellation);
                        continue;
                    }

                    foreach (var (_, trigger) in triggers)
                        trigger.Dispose();
                    triggers.Clear();

                    _visited.Add(_action);
                    var currentAction = _action;
                    _action = null;
                    _action = await currentAction.Execute(context, cancellation);
                } while (true);
            }
            finally
            {
                foreach (var (_, trigger) in triggers)
                    trigger.Dispose();
            }
        }

        static async void ObserveExceptions(Awaitable awaitable)
        {
            try
            {
                await awaitable;
            }
            catch (Exception e)
            {
                if (e is not OperationCanceledException)
                    Debug.LogException(e);
            }
        }

        public bool Visited(IPrerequisite node) => _visited.Contains(node);

        public IEnumerable<LocalizableText> GetLocalizableText()
        {
            foreach (var node in Nodes)
            {
                if (node is ILocalizableNode localizable)
                {
                    foreach (var localizableText in localizable.GetTextInstances())
                    {
                        yield return localizableText;
                    }
                }
            }
        }

        [ThreadStatic]
        private static HashSet<IAction>? _isNodeReachableVisitation;
        public bool IsNodeReachable(IAction thisAction, List<INodeValue>? path = null)
        {
            _isNodeReachableVisitation ??= new();
            _isNodeReachableVisitation.Clear();
            foreach (var node in Nodes)
            {
                if (node is Event e && e.Action is not null)
                {
                    path?.Add(e);
                    if (FindLeafAInBranchB(thisAction, e.Action, path))
                        return true;
                    path?.RemoveAt(path.Count-1);
                }

                static bool FindLeafAInBranchB(IAction target, IAction branch, List<INodeValue>? path)
                {
                    if (_isNodeReachableVisitation!.Add(branch) == false)
                        return false;

                    if (target == branch)
                    {
                        path?.Add(target);
                        return true;
                    }

                    path?.Add(branch);
                    foreach (IAction otherActions in branch.Followup())
                    {
                        if (FindLeafAInBranchB(target, otherActions, path))
                            return true;
                    }
                    path?.RemoveAt(path.Count-1);

                    return false;
                }
            }

            return false;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            __visitedEventsSerializationProxy.Clear();
            __visitedSerializationProxy.Clear();
            __visitedSerializationProxy.AddRange(_visited);
            __visitedEventsSerializationProxy.AddRange(_visitedEvents);

            var guids = new Dictionary<Guid, LocalizableText>();
            foreach (var localizableText in GetLocalizableText())
            {
                while (guids.TryGetValue(localizableText.Guid, out var existingInstance) && existingInstance != localizableText)
                {
                    localizableText.ForceRegenerateGuid();
                    Debug.LogWarning($"Duplicate Guid detected between '{localizableText.Content}' and '{existingInstance.Content}', regenerating Guid. This is standard behavior when copying nodes");
                }
                guids[localizableText.Guid] = localizableText;
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // This is to retain progress when reloading in play mode
            foreach (var prerequisite in __visitedSerializationProxy)
                _visited.Add(prerequisite);
            foreach (var e in __visitedEventsSerializationProxy)
                _visitedEvents.Add(e);
        }

#if UNITY_EDITOR
        static ScreenplayGraph()
        {
            UnityEditor.EditorApplication.playModeStateChanged += pmstc =>
            {
                if (pmstc is not (UnityEditor.PlayModeStateChange.ExitingEditMode or UnityEditor.PlayModeStateChange.ExitingPlayMode))
                {
                    return;
                }

                foreach (var screenplay in Resources.FindObjectsOfTypeAll<ScreenplayGraph>())
                {
                    if (screenplay.DebugRetainProgressInEditor)
                        continue;
                    screenplay._action = null;
                    screenplay.__visitedSerializationProxy.Clear();
                    screenplay.__visitedEventsSerializationProxy.Clear();
                    screenplay._visited.Clear();
                    screenplay._visitedEvents.Clear();
                }
            };
        }
#endif

        private record DefaultContext(ScreenplayGraph Source) : IContext, IDisposable
        {
            private Dictionary<object, CancellationTokenSource> _asynchronousRunner = new();
            private Component.UIBase? _dialogUI;

            public ScreenplayGraph Source { get; } = Source;

            public HashSet<IPrerequisite> Visited => Source._visited;

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

            public Component.UIBase? GetDialogUI()
            {
                return _dialogUI != null ? _dialogUI : _dialogUI = Instantiate(Source.DialogUIPrefab);
            }

            public void Dispose()
            {
                foreach (var (_, runner) in _asynchronousRunner)
                {
                    runner.Cancel();
                    runner.Dispose();
                }
                _asynchronousRunner.Clear();

                if (_dialogUI != null)
                    Destroy(_dialogUI.gameObject);
            }
        }
    }
}
