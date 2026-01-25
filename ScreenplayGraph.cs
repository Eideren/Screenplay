using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YNode;
using Screenplay.Nodes.Triggers;
using Event = Screenplay.Nodes.Event;
using Random = Unity.Mathematics.Random;
using static UnityEngine.Serialization.ManagedReferenceUtility;

namespace Screenplay
{
    [CreateAssetMenu(menuName = "Screenplay/Screenplay")]
    public class ScreenplayGraph : NodeGraph, ISerializationCallbackReceiver
    {
        public readonly List<Introspection> Introspections = new();

        public bool AllowMultipleInstances = false;
        public RestoreBehavior RestoreMode = RestoreBehavior.ReconstructLinearPaths;

        [PrefabWithComponent]
        public required Component.UIBase? DialogUIPrefab;
        public bool DebugRetainProgressInEditor;

        [SerializeField]
        private State _debugState = new();

        /// <summary>
        /// You must cancel this UniTask when reloading a running game.
        /// </summary>
        public async UniTask StartExecution(CancellationToken cancellation, uint seed, IntrospectionKey key)
        {
            if (Introspections.Count > 0 && AllowMultipleInstances == false)
                return;

            var introspection = new Introspection { Graph = this };
            key.BoundIntrospection = introspection;

            using var context = new DefaultContext(seed, introspection);
            var eventsReadySignal = AutoResetUniTaskCompletionSource.Create();
            var eventsCleanup = new Dictionary<Event, CancellationTokenSource>();
            using var busy = new LatentVariable<bool>(false);
            Introspections.Add(introspection);

            if (key.StateToLoad is not null)
                introspection.Progresses.AddRange(State.Reconstruct(key.StateToLoad, this, false));
            else if (DebugRetainProgressInEditor)
                introspection.Progresses.AddRange(State.Reconstruct(_debugState, this, false));

            try
            {
                RestoreProgress(introspection.Progresses, RestoreMode, introspection.Visited, cancellation, context);

                foreach (var value in Nodes)
                {
                    if (value is Event e && e.Action is not null && (e.Repeatable || introspection.Progresses.Find(x => x.Permutation.Event == e) is null))
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
                            lock (introspection.EventsReady)
                                introspection.EventsReady.Add((e, null));
                        }
                        else
                        {
                            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
                            eventsCleanup[e] = cts;
                            var task = triggerSource.Setup(new PreconditionCollector(OnUnlocked, OnLocked, busy, triggerSource, introspection), cts.Token);
                            ObserveExceptions(task);

                            void OnUnlocked(PreconditionCollector pc)
                            {
                                lock (introspection.EventsReady)
                                {
                                    introspection.EventsReady.Add((e, pc));
                                    eventsReadySignal.TrySetResult();
                                }
                            }

                            void OnLocked(PreconditionCollector pc)
                            {
                                lock (introspection.EventsReady)
                                    introspection.EventsReady.Remove((e, pc));
                            }
                        }
                    }

                    if (value is ICustomEntry customEntry)
                        customEntry.Run(context, cancellation);
                }

                while (cancellation.IsCancellationRequested == false)
                {
                    Event? eventToProcess;
                    lock (introspection.EventsReady)
                    {
                        if (introspection.EventsReady.Count != 0)
                        {
                            var ready = introspection.EventsReady[0];
                            introspection.EventsReady.RemoveAt(0);

                            context.Locals.Clear();
                            ready.collector?.SharedLocals.CopyTo(context.Locals);
                            eventToProcess = ready.Event;

                            if (eventToProcess.Repeatable)
                                introspection.EventsReady.Add(ready); // Move it to the end of the list
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

                    var progress = new EventProgress
                    {
                        Permutation = new VisitedPermutation{ Event = eventToProcess, Local = context.Locals.ToArray() }
                    };
                    introspection.Progresses.Add(progress);

                    busy.Set(true);

                    for (var executable = eventToProcess.Action; executable is not null; )
                    {
                        progress.Executables.Add(executable);
                        introspection.Visited.Add(executable);
                        var newE = await executable.InnerExecution(context, cancellation);
                        executable.Persistence(context, cancellation).Forget();
                        executable = newE;
                    }

                    busy.Set(false);
                }
            }
            finally
            {
                Introspections.Remove(introspection);
                eventsReadySignal.TrySetCanceled();
                if (DebugRetainProgressInEditor)
                    _debugState = State.CreateFrom(introspection);
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

            static void RestoreProgress(List<EventProgress> progresses, RestoreBehavior restoreBehavior, HashSet<IPrerequisite> visited, CancellationToken cancellationToken, DefaultContext defaultContext)
            {
                foreach (var eventProgress in progresses)
                {
                    if (eventProgress.Permutation.Event.Action == null)
                        continue;

                    var fork = new List<IExecutable>{ eventProgress.Permutation.Event.Action };

                    int head = 0;
                    var lastSessionList = eventProgress.Executables;
                    var reconstructedPath = new List<IExecutable>();
                    do
                    {
                        if (TrySkipOverOutdatedNodes(ref head, lastSessionList, fork, out var path))
                        {
                            reconstructedPath.Add(path);
                        }
                        else if (restoreBehavior == RestoreBehavior.StopOnFirstMismatch)
                        {
                            return;
                        }
                        else if (restoreBehavior == RestoreBehavior.ReconstructLinearPaths)
                        {
                            if (fork.Count > 1)
                                return;

                            reconstructedPath.Add(fork[0]);
                        }
                        else // No direct path, try to pick back up from further down this fork
                        {
                            var lastSession = new HashSet<IExecutable>(lastSessionList.ToArray()[head..]);
                            var executableVisited = new HashSet<IExecutable>(fork);
                            var pathsToTraverse = new List<List<IExecutable>>();
                            foreach (var executable in fork)
                                pathsToTraverse.Add(new List<IExecutable>{ executable });

                            do
                            {
                                // Check if any of the ends of the paths we're currently traversing match any of the
                                // paths we took in the previous session
                                if (pathsToTraverse.Find(pathToTest => lastSession.Contains(pathToTest[^1])) is { } matchingPath)
                                {
                                    // The tip of this path matches one of the last session's
                                    reconstructedPath.AddRange(matchingPath);
                                    head = lastSessionList.IndexOf(matchingPath[^1], head);
                                    goto FOUND_PATH;
                                }

                                int prevCount = pathsToTraverse.Count;
                                for (int i = 0; i < pathsToTraverse.Count; i++)
                                {
                                    foreach (var followup in pathsToTraverse[i][^1].Followup())
                                    {
                                        if (executableVisited.Add(followup))
                                            pathsToTraverse.Add(new List<IExecutable>(pathsToTraverse[i]) { followup });
                                    }
                                }
                                pathsToTraverse.RemoveRange(0, prevCount);
                            } while (pathsToTraverse.Count > 0);

                            // None of the rest of the nodes are part of all the branches, see if we can find a nice linear path to the end
                            while (fork.Count == 1)
                            {
                                reconstructedPath.Add(fork[0]);
                                fork.Clear();
                                fork.AddRange(reconstructedPath[^1].Followup());
                            }

                            if (fork.Count != 0 && restoreBehavior == RestoreBehavior.BestGuessPathUpToFirstNonVisitedFork)
                                return; // We're on a fork, let's bail from here

                            // Pick the first path regardless of
                            while (fork.Count != 0)
                            {
                                reconstructedPath.Add(fork[0]);
                                fork.Clear();
                                fork.AddRange(reconstructedPath[^1].Followup());
                            }

                            break;
                        }

                        FOUND_PATH:

                        fork.Clear();
                        fork.AddRange(reconstructedPath[^1].Followup());

                    } while (fork.Count > 0);

                    defaultContext.Locals.Clear();
                    foreach (var local in eventProgress.Permutation.Local)
                        defaultContext.Locals.TryAdd(local);

                    foreach (var executable in reconstructedPath)
                    {
                        visited.Add(executable);
                        executable.Persistence(defaultContext, cancellationToken).Forget();
                    }

                    static bool TrySkipOverOutdatedNodes(ref int head, List<IExecutable> queue, List<IExecutable> paths, [MaybeNullWhen(false)] out IExecutable path)
                    {
                        for (int i = head; i < queue.Count; i++)
                        {
                            if (paths.Contains(queue[i]))
                            {
                                head = i + 1;
                                path = queue[i];
                                return true;
                            }
                        }

                        path = null;
                        return false;
                    }
                }
            }
        }

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

        void ISerializationCallbackReceiver.OnAfterDeserialize() { }

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

            public Component.UIBase? GetDialogUI()
            {
                return _dialogUI != null ? _dialogUI : _dialogUI = Instantiate(Introspection.Graph.DialogUIPrefab);
            }
        }

        public class Introspection
        {
            public required ScreenplayGraph Graph;
            /// <summary> You must lock over this property to read it </summary>
            public readonly List<(Event Event, PreconditionCollector? collector)> EventsReady = new();
            public readonly Dictionary<Precondition, List<IPreconditionCollector>> Preconditions = new();
            public readonly HashSet<IPrerequisite> Visited = new();
            public readonly List<EventProgress> Progresses = new();
        }

        [Serializable]
        public class EventProgress
        {
            public required VisitedPermutation Permutation;
            [SerializeReference] public List<IExecutable> Executables = new();
        }

        public class IntrospectionKey
        {
            public Introspection? BoundIntrospection;
            public State? StateToLoad;
        }

        [Serializable]
        public class State
        {
            [Serializable]
            public class EventPlayback
            {
                public required GlobalId[] Local;
                public required long EventId;
                public List<long> Executables = new();
            }

            public List<EventPlayback> Events = new();

            public static State CreateFrom(Introspection introspection)
            {
                var stateOutput = new State();
                foreach (var progress in introspection.Progresses)
                {
                    var playback = new EventPlayback
                    {
                        Local = progress.Permutation.Local,
                        EventId = GetManagedReferenceIdForObject(introspection.Graph, progress.Permutation.Event)
                    };
                    foreach (var exe in progress.Executables)
                        playback.Executables.Add(GetManagedReferenceIdForObject(introspection.Graph, exe));
                    stateOutput.Events.Add(playback);
                }

                return stateOutput;
            }

            public static List<EventProgress> Reconstruct(State state, ScreenplayGraph graph, bool throwOnMissingEvents)
            {
                var ids = new HashSet<long>(GetManagedReferenceIds(graph));
                var output = new List<EventProgress>(state.Events.Count);
                foreach (var stateEvent in state.Events)
                {
                    if (ids.Contains(stateEvent.EventId) && GetManagedReference(graph, stateEvent.EventId) is Event e)
                    {
                        var progress = new EventProgress
                        {
                            Permutation = new VisitedPermutation
                            {
                                Event = e,
                                Local = stateEvent.Local,
                            }
                        };

                        foreach (var executableId in stateEvent.Executables)
                        {
                            var executable = GetManagedReference(graph, executableId) as IExecutable;
                            if (executable is null)
                                Debug.LogError($"Could not find executable from id {executableId}, screenplay {graph} will attempt to restore its state through {graph.RestoreMode}", graph);
                            progress.Executables.Add(executable!);
                        }

                        output.Add(progress);
                    }
                    else
                    {
                        string str = $"Could not find event from id {stateEvent.EventId}";
                        if (throwOnMissingEvents)
                            throw new InvalidOperationException(str);
                        else
                            Debug.LogError(str, graph);
                    }
                }

                return output;
            }
        }

        public enum RestoreBehavior
        {
            StopOnFirstMismatch,
            ReconstructLinearPaths,
            BestGuessPathUpToFirstNonVisitedFork,
            CompleteAllVisitedEventsPickRandomlyInForkIfWeHaveTo,
        }
    }
}
