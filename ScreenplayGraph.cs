using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YNode;
using Screenplay.Nodes;
using Screenplay.Nodes.Triggers;
using Sirenix.OdinInspector;
using Event = Screenplay.Nodes.Event;
using Random = Unity.Mathematics.Random;

namespace Screenplay
{
    [CreateAssetMenu(menuName = "Screenplay/Screenplay")]
    public class ScreenplayGraph : NodeGraph, ISerializationCallbackReceiver
    {
        public required Component.UIBase? DialogUIPrefab;
        public bool DebugRetainProgressInEditor;
        private HashSet<IPrerequisite> _visiting = new();
        private Queue<IScreenplayNode?> _orderedVisitation = new();
        private HashSet<Event> _visitedEvents = new();

        [SerializeField, HideInInspector, SerializeReference] private List<IPrerequisite> __visitedSerializationProxy = new ();
        [SerializeField, HideInInspector, SerializeReference] private List<IScreenplayNode?> __orderedVisitationSerializationProxy = new ();
        [SerializeField, HideInInspector, SerializeReference] private List<Event> __visitedEventsSerializationProxy = new ();

        /// <summary>
        /// You must dispose of this UniTask when reloading a running game.
        /// </summary>
        public async UniTask StartExecution(CancellationToken cancellation, uint seed)
        {
            using var context = new DefaultContext(this, seed);
            var events = new List<Event>();
            foreach (var value in Nodes)
            {
                if (value is Event e && e.Action is not null && (e.Repeatable || _visitedEvents.Contains(e) == false))
                    events.Add(e);
                if (value is ICustomEntry customEntry)
                    customEntry.Run(this, context, cancellation);
            }

            while (cancellation.IsCancellationRequested == false)
            {
                var (currentEvent, additionalContext) = await AwaitAnyEvent(events, context, cancellation);

                _visitedEvents.Add(currentEvent);
                if (currentEvent.Repeatable == false)
                    events.Remove(currentEvent);

                additionalContext?.AppendTo(context);
                await currentEvent.Action.Execute(context, cancellation);
                context.Annotations.Clear();
            }
        }

        private async UniTask<(Event, IAnnotation?)> AwaitAnyEvent(List<Event> events, IEventContext context, CancellationToken cancellation)
        {
            (Event, IAnnotation?)? fromTrigger = null;
            var triggerables = new Dictionary<Event, CancellationTokenSource>();
            try
            {
                do
                {
                    // Check if any non triggerable can be run
                    foreach (var e in events)
                    {
                        if (e.TriggerSource is not null)
                            continue;
                        if (e.Prerequisite?.TestPrerequisite(context) == false)
                            continue;
                        if (e.Action is null)
                            continue;
                        return (e, null);
                    }

                    // Now check only for triggerable, and set them up if they haven't been
                    foreach (var e in events)
                    {
                        if (e.Action is null)
                            continue;
                        if (e.TriggerSource is null)
                            continue;

                        if (e.Prerequisite?.TestPrerequisite(context) == false)
                        {
                            if (triggerables.Remove(e, out var cts)) // Prerequisites are invalid, we must cancel the trigger
                            {
                                cts.Cancel();
                                cts.Dispose();
                            }

                            continue;
                        }

                        if (triggerables.ContainsKey(e) == false)
                        {
                            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
                            triggerables.Add(e, cts);
                            SetFromTriggerWhenSignaled(e, cts.Token);
                        }
                    }

                    if (fromTrigger is {} v)
                        return v;

                    await UniTask.NextFrame(cancellation, cancelImmediately: true);

                    if (fromTrigger is {} v2)
                        return v2;

                } while (cancellation.IsCancellationRequested == false);
            }
            finally
            {
                foreach (var cancellationTokenSource in triggerables.ToArray())
                {
                    if (triggerables.Remove(cancellationTokenSource.Key, out _))
                    {
                        cancellationTokenSource.Value.Cancel();
                        cancellationTokenSource.Value.Dispose();
                    }
                }
            }

            throw new OperationCanceledException();

            async void SetFromTriggerWhenSignaled(Event e, CancellationToken token)
            {
                try
                {
                    var ctx = await e.TriggerSource!.AwaitTrigger(token);
                    fromTrigger = (e, ctx);
                }
                catch (Exception ex)
                {
                    if (ex is not OperationCanceledException)
                        Debug.LogException(ex);
                }
                finally
                {
                    if (triggerables.Remove(e, out CancellationTokenSource value))
                    {
                        value.Cancel();
                        value.Dispose();
                    }
                }
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
                    screenplay.__orderedVisitationSerializationProxy.Clear();
                    screenplay.__visitedSerializationProxy.Clear();
                    screenplay.__visitedEventsSerializationProxy.Clear();
                    screenplay._visiting.Clear();
                    screenplay._visitedEvents.Clear();
                }
            };
        }
#endif

        private record DefaultContext : IEventContext, IDisposable
        {
            private Component.UIBase? _dialogUI;
            private Random _random;

            public List<IAnnotation> Annotations { get; set; } = new();
            public ScreenplayGraph Source { get; }

            public DefaultContext(ScreenplayGraph Source, uint seed)
            {
                this.Source = Source;
                _random = new Random(seed);
            }

            public void Dispose()
            {
                if (_dialogUI != null)
                    Destroy(_dialogUI.gameObject);
            }

            public void Visiting(IBranch? executable)
            {
                Source._orderedVisitation.Enqueue(executable);
                if (executable is IPrerequisite prerequisite)
                    Source._visiting.Add(prerequisite);
            }

            public uint NextSeed() => _random.NextUInt(1, uint.MaxValue);

            public bool Visited(IPrerequisite executable) => Source._visiting.Contains(executable);

            public Component.UIBase? GetDialogUI()
            {
                return _dialogUI != null ? _dialogUI : _dialogUI = Instantiate(Source.DialogUIPrefab);
            }
        }
    }
}
