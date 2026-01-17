using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YNode;
using Screenplay.Nodes.Triggers;
using Event = Screenplay.Nodes.Event;
using Random = Unity.Mathematics.Random;

namespace Screenplay
{
    [CreateAssetMenu(menuName = "Screenplay/Screenplay")]
    public class ScreenplayGraph : NodeGraph, ISerializationCallbackReceiver
    {
        public readonly List<Introspection> Introspections = new();

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
            var eventsReady = new List<(Event Event, PreconditionCollector? collector)>();
            var introspection = new Introspection
            {
                Graph = this,
                EventsReady = eventsReady,
            };

            using var context = new DefaultContext(seed, introspection);
            var eventsReadySignal = AutoResetUniTaskCompletionSource.Create();
            var eventsCleanup = new Dictionary<Event, CancellationTokenSource>();
            using var busy = new LatentVariable<bool>(false);
            Introspections.Add(introspection);

            try
            {
                foreach (var value in Nodes)
                {
                    if (value is Event e && e.Action is not null && (e.Repeatable || _visited.ContainsKey(e) == false))
                    {
                        var triggerSource = e.TriggerSource;
                        if (e.Scene.IsValid())
                        {
                            var sceneLoadedTrigger = new WhileSceneLoaded { Target = e.Scene };
                            if (triggerSource != null)
                                triggerSource = new WhenAll { Sources = new[] { sceneLoadedTrigger, triggerSource } };
                            else
                                triggerSource = sceneLoadedTrigger;
                        }

                        if (triggerSource == null)
                        {
                            lock (eventsReady)
                                eventsReady.Add((e, null));
                        }
                        else
                        {
                            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
                            eventsCleanup[e] = cts;
                            var task = triggerSource.Setup(new PreconditionCollector(OnUnlocked, OnLocked, busy, triggerSource, introspection), cts.Token);
                            ObserveExceptions(task);

                            void OnUnlocked(PreconditionCollector pc)
                            {
                                lock (eventsReady)
                                {
                                    eventsReady.Add((e, pc));
                                    eventsReadySignal.TrySetResult();
                                }
                            }

                            void OnLocked(PreconditionCollector pc)
                            {
                                lock (eventsReady)
                                    eventsReady.Remove((e, pc));
                            }
                        }
                    }

                    if (value is ICustomEntry customEntry)
                        customEntry.Run(context, cancellation);
                }

                while (cancellation.IsCancellationRequested == false)
                {
                    Event? eventToProcess;
                    lock (eventsReady)
                    {
                        if (eventsReady.Count != 0)
                        {
                            var ready = eventsReady[0];
                            eventsReady.RemoveAt(0);

                            context.Locals.Clear();
                            ready.collector?.SharedLocals.CopyTo(context.Locals);
                            eventToProcess = ready.Event;

                            if (eventToProcess.Repeatable)
                                eventsReady.Add(ready); // Move it to the end of the list
                            else if (eventsCleanup.Remove(eventToProcess, out var triggerSource))
                                triggerSource.Cancel(); // Otherwise dispose of the trigger and be done with it
                        }
                        else
                        {
                            eventToProcess = null;
                        }
                    }

                    if (eventToProcess == null)
                    {
                        await eventsReadySignal.Task.WithInterruptingCancellation(cancellation);
                        continue;
                    }

                    _visited.Add(eventToProcess, new VisitedPermutation{ Event = eventToProcess, Local = context.Locals.ToArray() });

                    busy.Set(true);

                    await eventToProcess.Action.Execute(context, cancellation);

                    busy.Set(false);
                }
            }
            finally
            {
                Introspections.Remove(introspection);
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

            {
                var guids = new Dictionary<Guid, LocalizableText>();
                foreach (var localizableText in GetLocalizableText())
                {
                    while (guids.TryGetValue(localizableText.Guid, out var existingInstance) && existingInstance != localizableText)
                    {
                        localizableText.ForceRegenerateGuid();
                        Debug.LogWarning($"Duplicate Guid detected between '{localizableText.Content}' and '{existingInstance.Content}', regenerating Guid. This is expected when copying nodes");
                    }
                    guids[localizableText.Guid] = localizableText;
                }
            }

            {
                var guids = new Dictionary<Guid, IUntypedGlobalsDeclarer>();
                foreach (var node in Nodes)
                {
                    if (node is not IUntypedGlobalsDeclarer local)
                        continue;

                    // Check that the proxy is a real proxy, that is that it doesn't point at itself
                    if (node is IUntypedGlobalsProxy p && p != p.ProxyTarget)
                        continue;

                    while (guids.TryGetValue(local.Guid, out var existingInstance) && existingInstance != local)
                    {
                        local.Guid = guid.New();
                        Debug.LogWarning($"Duplicate Guid detected between '{existingInstance}' and '{local}', regenerating Guid. This is expected when copying nodes");
                    }
                    guids[local.Guid] = local;
                }
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

            public Locals Locals { get; } = new();

            public Introspection Introspection { get; }

            public DefaultContext(uint seed, Introspection introspection)
            {
                _random = new Random(seed);
                Introspection = introspection;
            }

            public void Dispose()
            {
                if (_dialogUI != null)
                    Destroy(_dialogUI.gameObject);
            }

            public ref Random GetRandom() => ref _random;

            public void Visiting(IBranch? executable)
            {
                Introspection.Graph._orderedVisitation.Enqueue(executable);
                if (executable is IPrerequisite prerequisite)
                    Introspection.Graph._visiting.Add(prerequisite);
            }

            public bool Visited(IPrerequisite executable) => Introspection.Graph._visiting.Contains(executable);

            public Component.UIBase? GetDialogUI()
            {
                return _dialogUI != null ? _dialogUI : _dialogUI = Instantiate(Introspection.Graph.DialogUIPrefab);
            }
        }

        public class Introspection
        {
            public required ScreenplayGraph Graph;
            public required List<(Event Event, PreconditionCollector? collector)> EventsReady;
            public Dictionary<Precondition, List<IPreconditionCollector>> Preconditions = new();
        }
    }
}
