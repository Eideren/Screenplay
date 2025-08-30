using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        private HashSet<IPrerequisite> _visiting = new();
        private Queue<IScreenplayNode?> _orderedVisitation = new();
        private HashSet<Event> _visitedEvents = new();

        // Only serialized for editor reloading purposes
        [SerializeField, HideInInspector] private Event? _event;
        [SerializeField, HideInInspector, SerializeReference] private List<IPrerequisite> __visitedSerializationProxy = new ();
        [SerializeField, HideInInspector, SerializeReference] private List<IScreenplayNode?> __orderedVisitationSerializationProxy = new ();
        [SerializeField, HideInInspector, SerializeReference] private List<Event> __visitedEventsSerializationProxy = new ();

        /// <summary>
        /// You must dispose of this UniTask when reloading a running game.
        /// </summary>
        public async UniTask StartExecution(CancellationToken cancellation)
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
                        customEntry.Run(this, context, cancellation);
                }

                do
                {
                    if (_event is null) // Check non-triggerable
                    {
                        foreach (var e in events)
                        {
                            if (e.TriggerSource is not null)
                                continue;
                            if (e.Prerequisite?.TestPrerequisite(context) == false)
                                continue;
                            if (e.Action is null)
                                continue;

                            _visitedEvents.Add(e);
                            if (e.Repeatable == false)
                                events.Remove(e);
                            _event = e;
                            break;
                        }
                    }

                    if (_event is null) // Check triggerable
                    {
                        foreach (var e in events)
                        {
                            if (e.Action is null)
                                continue;
                            if (e.TriggerSource is null)
                                continue;

                            if (e.Prerequisite?.TestPrerequisite(context) == false)
                            {
                                if (triggers.TryGetValue(e, out var outdatedTrigger))
                                    outdatedTrigger.Dispose();
                                continue;
                            }

                            if (triggers.ContainsKey(e))
                                continue;

                            System.Action callback = () =>
                            {
                                if (_event is not null)
                                    return; // Another trigger already queued one

                                _visitedEvents.Add(e);
                                if (e.Repeatable == false)
                                    events.Remove(e);
                                _event = e;
                            };

                            if (e.TriggerSource.TryCreateTrigger(callback, out var trigger) == false)
                                continue;

                            triggers.Add(e, trigger);
                            break;
                        }
                    }

                    if (_event is null)
                    {
                        await UniTask.NextFrame(cancellation, cancelImmediately:true);
                        continue;
                    }

                    foreach (var (_, trigger) in triggers)
                        trigger.Dispose();
                    triggers.Clear();

                    var currentEvent = _event;
                    _event = null;
                    await currentEvent.Action.Execute(context, cancellation);
                } while (true);
            }
            finally
            {
                foreach (var (_, trigger) in triggers)
                    trigger.Dispose();
            }
        }

        public bool Visited(IPrerequisite node) => _visiting.Contains(node);

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
        private static HashSet<IBranch>? _isNodeReachableVisitation;
        public bool IsNodeReachable(IBranch thisExecutable, List<IScreenplayNode>? path = null)
        {
            _isNodeReachableVisitation ??= new();
            _isNodeReachableVisitation.Clear();
            foreach (var node in Nodes)
            {
                if (node is IBranch b && node is Event or ICustomEntry)
                {
                    path?.Add(b);
                    foreach (var branch in b.Followup())
                    {
                        if (branch is not null && FindLeafAInBranchB(thisExecutable, branch, path))
                            return true;
                    }
                    path?.RemoveAt(path.Count-1);
                }

                static bool FindLeafAInBranchB(IBranch target, IBranch branch, List<IScreenplayNode>? path)
                {
                    if (_isNodeReachableVisitation!.Add(branch) == false)
                        return false;

                    if (target == branch)
                    {
                        path?.Add(target);
                        return true;
                    }

                    path?.Add(branch);
                    foreach (var otherActions in branch.Followup())
                    {
                        if (otherActions is null)
                            continue;
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
            __orderedVisitationSerializationProxy.Clear();
            __orderedVisitationSerializationProxy.AddRange(_orderedVisitation);
            __visitedSerializationProxy.AddRange(_visiting);
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
            foreach (var screenplayNodeValue in __orderedVisitationSerializationProxy)
                _orderedVisitation.Enqueue(screenplayNodeValue);
            foreach (var prerequisite in __visitedSerializationProxy)
                _visiting.Add(prerequisite);
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
                    screenplay._event = null;
                    screenplay.__orderedVisitationSerializationProxy.Clear();
                    screenplay.__visitedSerializationProxy.Clear();
                    screenplay.__visitedEventsSerializationProxy.Clear();
                    screenplay._visiting.Clear();
                    screenplay._visitedEvents.Clear();
                }
            };
        }
#endif

        private record DefaultContext(ScreenplayGraph Source) : IEventContext, IDisposable
        {
            private Dictionary<object, CancellationTokenSource> _asynchronousRunner = new();
            private Component.UIBase? _dialogUI;

            public ScreenplayGraph Source { get; } = Source;

            public Queue<IScreenplayNode?> OrderedVisitation => Source._orderedVisitation;


            public void Visiting(IBranch? executable)
            {
                OrderedVisitation.Enqueue(executable);
                if (executable is IPrerequisite prerequisite)
                    Source._visiting.Add(prerequisite);
            }

            public bool Visited(IPrerequisite executable) => Source._visiting.Contains(executable);

            public void RunAsynchronously(object key, Func<CancellationToken, UniTask> runner)
            {
                StopAsynchronous(key);
                var source = new CancellationTokenSource();
                _ = RunAndDiscard(key, source.Token, runner);
                _asynchronousRunner.Add(key, source);
            }

            async UniTask RunAndDiscard(object key, CancellationToken cancellation, Func<CancellationToken, UniTask> runner)
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
