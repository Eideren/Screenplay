using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YNode;
using Screenplay.Nodes;
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
        private Dictionary<Event, VisitedPermutation> _visited = new();

        [SerializeField, HideInInspector, SerializeReference] private List<IPrerequisite> __visitedSerializationProxy = new ();
        [SerializeField, HideInInspector, SerializeReference] private List<IScreenplayNode?> __orderedVisitationSerializationProxy = new ();
        [SerializeField, HideInInspector, SerializeReference] private List<VisitedPermutation> __visitedEventsSerializationProxy = new ();

        /// <summary>
        /// You must cancel this UniTask when reloading a running game.
        /// </summary>
        public async UniTask StartExecution(CancellationToken cancellation, uint seed)
        {
            using var context = new DefaultContext(this, seed);
            var eventsReady = new List<VisitedPermutation>();
            var eventsReadySignal = AutoResetUniTaskCompletionSource.Create();
            var eventsCleanup = new Dictionary<Event, CancellationTokenSource>();
            var runnerState = new EventRunnerState();

            try
            {
                foreach (var value in Nodes)
                {
                    if (value is Event e && e.Action is not null && (e.Repeatable || _visited.ContainsKey(e) == false))
                    {
                        if (e.TriggerSource == null)
                        {
                            lock (eventsReady)
                                eventsReady.Add(new VisitedPermutation { Event = e, Variants = Array.Empty<(VariantBase, guid)>() });
                        }
                        else
                        {
                            var eventTracker = new EventTracker(eventsReady, eventsReadySignal, e, runnerState);
                            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
                            eventsCleanup[e] = cts;
                            var task = e.TriggerSource.Setup(eventTracker, cts.Token);
                            ObserveExceptions(task);
                        }
                    }

                    if (value is ICustomEntry customEntry)
                        customEntry.Run(this, context, cancellation);
                }

                while (cancellation.IsCancellationRequested == false)
                {
                    VisitedPermutation permutation;
                    while (true) // Pick next event
                    {
                        UniTask eventsReadyTask;
                        lock (eventsReady)
                        {
                            if (eventsReady.Count != 0)
                            {
                                permutation = eventsReady[0];
                                eventsReady.RemoveAt(0);
                                if (permutation.Event.Repeatable)
                                    eventsReady.Add(permutation); // Move it to the end of the list
                                else if (eventsCleanup.Remove(permutation.Event, out var triggerSource))
                                    triggerSource.Cancel(); // Otherwise dispose of the trigger and be done with it

                                break;
                            }

                            eventsReadyTask = eventsReadySignal.Task;
                        }

                        await UniTask.WhenAny(eventsReadyTask, UniTask.WaitUntilCanceled(cancellation, completeImmediately: true));

                        cancellation.ThrowIfCancellationRequested();
                    }

                    _visited.Add(permutation.Event, permutation);

                    foreach (var (variant, guid) in permutation.Variants)
                        context.Variants[variant] = guid;

                    runnerState.IsRunningEvent = true;
                    runnerState.StateChanged.TrySetResult();

                    await permutation.Event.Action.Execute(context, cancellation);

                    runnerState.IsRunningEvent = false;
                    runnerState.StateChanged.TrySetResult();

                    context.Variants.Clear();
                }
            }
            finally
            {
                runnerState.StateChanged.TrySetCanceled();
                eventsReadySignal.TrySetCanceled();
            }

            static async void ObserveExceptions(UniTask uniTask)
            {
                try
                {
                    await uniTask;
                }
                catch (Exception e)
                {
                    if (e is not OperationCanceledException)
                        Debug.LogException(e);
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
            __visitedEventsSerializationProxy.AddRange(_visited.Values);

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
                _visited.Add(e.Event, e);
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
                    screenplay._visited.Clear();
                }
            };
        }
#endif

        private record DefaultContext : IEventContext, IDisposable
        {
            private Component.UIBase? _dialogUI;
            private Random _random;

            public Dictionary<VariantBase, guid> Variants { get; set; } = new();

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
