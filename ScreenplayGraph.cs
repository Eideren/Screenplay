using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Screenplay.Nodes;
using UnityEngine;
using YNode;
using Screenplay.Nodes.Triggers;
using Sirenix.OdinInspector;
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
        public RestoreBehavior RestoreMode = RestoreBehavior.NoRestriction;
        public bool DebugRetainProgressInEditor;

        [SerializeField]
        private State _debugState = new();

        [SerializeReference, InlineProperty, ListDrawerSettings(DefaultExpandedState = true)]
        public required ICustomField[] Fields = { new DialogUIField{ DialogUIPrefab = null! } };

        /// <summary>
        /// You must cancel this UniTask when reloading a running game.
        /// </summary>
        public async UniTask StartExecution(CancellationToken cancellation, uint seed, IntrospectionKey key)
        {
            if (Introspections.Count > 0 && AllowMultipleInstances == false)
                return;

            var introspection = new Introspection { Graph = this };
            key.BoundIntrospection = introspection;

            using var fieldRegistry = new FieldRegistry(this);
            var eventsReadySignal = new SafeManualResetEvent();
            var eventsCleanup = new Dictionary<Event, CancellationTokenSource>();
            Introspections.Add(introspection);

            State? state;
            if (key.StateToLoad is not null)
                state = key.StateToLoad;
            else if (DebugRetainProgressInEditor)
                state = _debugState;
            else
                state = null;

            var randomSeeder = new Random(seed);
            try
            {
                if (state is not null)
                {
                    var reconstructed = State.Reconstruct(state, RestoreMode, out RestoreBehavior effects, this);
                    foreach (var eventProgress in reconstructed)
                    {
                        var context = new DefaultContext(seed, introspection, fieldRegistry);
                        context.Locals.Clear();
                        foreach (var local in eventProgress.Permutation.Local)
                            context.Locals.TryAdd(local);

                        introspection.Visited.Add(eventProgress.First);
                        eventProgress.First.Persistence(context, cancellation).Forget();

                        foreach (var executable in eventProgress.ExecutionOrder)
                        {
                            introspection.Visited.Add(executable.Next);
                            executable.Next.Persistence(context, cancellation).Forget();
                        }
                    }
                    introspection.Progresses.AddRange(reconstructed);
                }

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
                            {
                                introspection.EventsReady.Add((e, null));
                                eventsReadySignal.Open();
                            }
                        }
                        else
                        {
                            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
                            eventsCleanup[e] = cts;
                            var task = triggerSource.Setup(new PreconditionCollector(OnUnlocked, OnLocked, triggerSource, introspection), cts.Token);
                            task.Forget();

                            void OnUnlocked(PreconditionCollector pc)
                            {
                                lock (introspection.EventsReady)
                                {
                                    introspection.EventsReady.Add((e, pc));
                                    eventsReadySignal.Open();
                                }
                            }

                            void OnLocked(PreconditionCollector pc)
                            {
                                lock (introspection.EventsReady)
                                {
                                    introspection.EventsReady.Remove((e, pc));
                                    if (introspection.EventsReady.Count == 0)
                                        eventsReadySignal.Close();
                                }
                            }
                        }
                    }

                    if (value is ICustomEntry customEntry)
                        customEntry.Run(new DefaultContext(randomSeeder.NextUInt(1, uint.MaxValue), introspection, fieldRegistry), cancellation);
                }

                while (cancellation.IsCancellationRequested == false)
                {
                    var context = new DefaultContext(randomSeeder.NextUInt(1, uint.MaxValue), introspection, fieldRegistry);
                    Event? eventToProcess;
                    lock (introspection.EventsReady)
                    {
                        if (introspection.EventsReady.Count != 0)
                        {
                            var ready = introspection.EventsReady[0];
                            introspection.EventsReady.RemoveAt(0);
                            if (introspection.EventsReady.Count == 0)
                                eventsReadySignal.Close();

                            context.Locals.Clear();
                            ready.collector?.SharedLocals.CopyTo(context.Locals);
                            eventToProcess = ready.Event;

                            if (eventToProcess.Repeatable)
                            {
                                introspection.EventsReady.Add(ready); // Move it to the end of the list
                                eventsReadySignal.Open();
                            }
                            else if (eventsCleanup.Remove(eventToProcess, out var triggerSource))
                            {
                                triggerSource.Cancel(); // Otherwise dispose of the trigger and be done with it
                                triggerSource.Dispose();
                            }
                        }
                        else
                        {
                            eventToProcess = null;
                        }
                    }

                    if (eventToProcess?.Action is null)
                    {
                        await eventsReadySignal.AwaitOpen.WithInterruptingCancellation(cancellation);
                        continue;
                    }

                    var progress = new EventProgress
                    {
                        First = eventToProcess.Action,
                        Permutation = new VisitedPermutation{ Event = eventToProcess, Local = context.Locals.ToArray() }
                    };
                    introspection.Progresses.Add(progress);

                    for (IExecutable? executable = eventToProcess.Action, nextExecutable; executable is not null; executable = nextExecutable)
                    {
                        introspection.Visited.Add(executable);

                        if (executable is IExecutableSimultaneously ies)
                        {
                            nextExecutable = await HandleSimultaneousExecutable(ies, progress, context, cancellation);
                        }
                        else
                        {
                            nextExecutable = await executable.Execute(context, cancellation);
                            executable.Persistence(context, cancellation).Forget();
                            if (nextExecutable is not null)
                                progress.ExecutionOrder.Add(new(executable, nextExecutable));
                        }
                    }
                }
            }
            finally
            {
                Introspections.Remove(introspection);
                eventsReadySignal.TrySetCanceled();
                if (DebugRetainProgressInEditor)
                    _debugState = State.CreateFrom(introspection);
            }
        }

        private static UniTask<IExecutable?> HandleSimultaneousExecutable(IExecutableSimultaneously ies, EventProgress progress, IEventContext context, CancellationToken cancellation)
        {
            var entries = ies.Followup().Where(x => x != null).ToList();
            var doneSignal = new UniTaskCompletionSource<IExecutable?>();
            IExecutableJoin? expectedJoin = null;
            int leftToDo = entries.Count;

            foreach (var entry in entries)
            {
                progress.ExecutionOrder.Add(new(ies, entry!));
                ParallelTask(entry!).Forget();
            }

            return doneSignal.Task.WithInterruptingCancellation(cancellation);

            async UniTask ParallelTask(IExecutable? executable)
            {
                try
                {
                    for (IExecutable? nextExecutable; executable is not null; executable = nextExecutable)
                    {
                        switch (executable)
                        {
                            case IExecutableJoin iJoin when expectedJoin is null:
                                expectedJoin = iJoin;
                                return; // We're done, let screenplay continue on from this join
                            case IExecutableJoin iJoin when expectedJoin == iJoin: return; // We reached the same join as the preceding branch
                            case IExecutableJoin iJoin: throw new InvalidOperationException($"Reached a different join ({iJoin} / {expectedJoin}) while originating from the same {ies}");
                            case IExecutableSimultaneously ies:
                                nextExecutable = await HandleSimultaneousExecutable(ies, progress, context, cancellation);
                                break;
                            default:
                                nextExecutable = await executable.Execute(context, cancellation);
                                executable.Persistence(context, cancellation).Forget();
                                if (nextExecutable is not null)
                                    progress.ExecutionOrder.Add(new(executable, nextExecutable));
                                break;
                        }
                    }
                }
                finally
                {
                    if (Interlocked.Decrement(ref leftToDo) == 0)
                    {
                        doneSignal.TrySetResult(expectedJoin);
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

        private record DefaultContext : IEventContext
        {
            private Random _random;

            public FieldRegistry FieldRegistry { get; }

            public Locals Locals { get; } = new();

            public Introspection Introspection { get; }

            public DefaultContext(uint seed, Introspection introspection, FieldRegistry fieldRegistry)
            {
                _random = new Random(seed);
                Introspection = introspection;
                FieldRegistry = fieldRegistry;
            }

            public ref Random GetRandom() => ref _random;
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
            public required IExecutable First;
            [SerializeReference] public List<Link> ExecutionOrder = new();
        }

        public record struct Link(IExecutable Previous, IExecutable Next);

        [Serializable]
        public record struct ExecutableSerialized(long Previous, long Next)
        {
            public long Previous = Previous;
            public long Next = Next;
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
                public required long FirstExecutable;
                public List<ExecutableSerialized> Executables = new();
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
                        EventId = GetManagedReferenceIdForObject(introspection.Graph, progress.Permutation.Event),
                        FirstExecutable = GetManagedReferenceIdForObject(introspection.Graph, progress.First),
                    };
                    foreach (var exe in progress.ExecutionOrder)
                    {
                        long prev = GetManagedReferenceIdForObject(introspection.Graph, exe.Previous);
                        long next = GetManagedReferenceIdForObject(introspection.Graph, exe.Next);
                        playback.Executables.Add(new(prev, next));
                    }
                    stateOutput.Events.Add(playback);
                }

                return stateOutput;
            }

            public static List<EventProgress> Reconstruct(State oldStates, RestoreBehavior stopAt, out RestoreBehavior affectedBehavior, ScreenplayGraph graph)
            {
                affectedBehavior = default;

                var refToId = GetManagedReferenceIds(graph).ToDictionary(x => GetManagedReference(graph, x));
                var idToRef = refToId.ToDictionary(x => x.Value, x => x.Key);
                var output = new List<EventProgress>(oldStates.Events.Count);
                foreach (var oldState in oldStates.Events)
                {
                    if (idToRef.TryGetValue(oldState.EventId, out object? node) == false || node is not Event e || e.Action is null)
                    {
                        Debug.LogError($"Could not find event from id {oldState.EventId}", graph);
                        affectedBehavior |= RestoreBehavior.EventNotFound;
                        if ((stopAt & RestoreBehavior.EventNotFound) != 0)
                            break;

                        continue;
                    }

                    var progress = new EventProgress
                    {
                        First = e.Action,
                        Permutation = new VisitedPermutation
                        {
                            Event = e,
                            Local = oldState.Local,
                        }
                    };

                    var nodesLeftInState = oldState.Executables
                        .Select(x => x.Previous)
                        .Append(oldState.Executables[^1].Next)
                        .Select(x => idToRef.GetValueOrDefault(x) as IExecutable)
                        .NotNull()
                        .ToList();

                    var branches = new List<PathWalker> { new(new List<object>(), e.Action) };

                    do
                    {
                        (List<IExecutable> links, PathWalker branch)? pathQuery = null;
                        foreach (var path in branches)
                        {
                            if (FindShortestPathTo(path.Current, nodesLeftInState) is { } pathFound && (pathQuery is null || pathQuery.Value.links.Count > pathFound.Count))
                            {
                                pathQuery = (pathFound, path);
                            }
                        }

                        if (pathQuery is not { } q)
                        {
                            affectedBehavior |= RestoreBehavior.NodeNotInState;
                            if ((stopAt & RestoreBehavior.NodeNotInState) != 0)
                                return output;
                            break;
                        }

                        var (links, bestBranch) = q;

                        Debug.Assert(bestBranch.Current == links[0]);

                        for (int i = 0; i < links.Count; i++)
                        {
                            var current = links[i];
                            nodesLeftInState.Remove(current);

                            if (TryAdvanceTo(current, i + 1 < links.Count ? links[i + 1] : null, progress, bestBranch, branches) == false)
                                break; // Stop moving along this path, the other path will take over
                        }
                    } while (branches.Count > 0);

                    while (branches.Count > 0)
                    {
                        var branch = branches[^1];
                        var followup = branch.Current.Followup().FirstOrDefault(x => x is not null);
                        if (followup is null)
                        {
                            branches.Remove(branch);
                        }
                        else
                        {
                            TryAdvanceTo(followup, null, progress, branch, branches);
                        }
                    }

                    if (nodesLeftInState.Count > 0)
                    {
                        affectedBehavior |= RestoreBehavior.NodeNotInGraph;
                        if ((stopAt & RestoreBehavior.NodeNotInGraph) != 0)
                            return output;
                    }

                    output.Add(progress);
                }

                return output;

                static List<IExecutable>? FindShortestPathTo(IExecutable from, List<IExecutable> targets)
                {
                    if (targets.Contains(from))
                        return new List<IExecutable> { from };

                    var branches = new HashSet<Link>();
                    var nodeBacklink = new Dictionary<IExecutable, IExecutable>();
                    var pathsToTraverse = new Queue<(IExecutable? previous, IExecutable current)>();
                    pathsToTraverse.Enqueue((null, from));

                    while (pathsToTraverse.TryDequeue(out var path))
                    {
                        if (path.previous is not null)
                        {
                            if (branches.Add(new Link(path.previous, path.current)) == false)
                                continue;

                            nodeBacklink.TryAdd(path.current, path.previous); // we only keep the shortest path, i.e.: the first link that reaches this node
                        }

                        // When presented with a branch and multiple nodes that have been traversed, we pick the result which occurs the soonest
                        int? bestMatch = null;
                        foreach (var p in path.current.Followup())
                        {
                            if (p is not null)
                            {
                                pathsToTraverse.Enqueue((path.current, p));
                                if (targets.IndexOf(p) is var i && i != -1 && (bestMatch is null || bestMatch.Value > i))
                                {
                                    bestMatch = i;
                                }
                            }
                        }

                        if (bestMatch != null)
                        {
                            var output = new List<IExecutable> { targets[bestMatch.Value], path.current };

                            for (var current = path.current;
                                 nodeBacklink.TryGetValue(current, out var previous);
                                 current = previous)
                            {
                                output.Add(previous);
                            }

                            // Caller wants a path from -> target, but we add them in reverse, so we reverse the list
                            output.Reverse();
                            return output;
                        }
                    }

                    return null;
                }

                static bool TryAdvanceTo(IExecutable current, IExecutable? preferredNext, EventProgress progress, PathWalker bestBranch, List<PathWalker> branches)
                {
                    bestBranch.SetCurrent(current, progress.ExecutionOrder);

                    if (current is IExecutableSimultaneously simultaneous)
                    {
                        var keyForThisSimultaneous = new object();

                        bool found = false;
                        foreach (var followup in simultaneous.Followup())
                        {
                            if (preferredNext is not null && preferredNext == followup)
                            {
                                // The current path we're traversing goes through this node, this followup will be taken care of within the outer loop
                                found = true;
                            }
                            else if (followup is not null)
                            {
                                var pathWalker = new PathWalker(new(bestBranch.SimultaneousToClose) { keyForThisSimultaneous }, simultaneous);
                                pathWalker.SetCurrent(followup, progress.ExecutionOrder);
                                branches.Add(pathWalker);
                            }
                        }

                        if (preferredNext is null)
                        {
                            branches.Remove(bestBranch);
                            return false;
                        }

                        Debug.Assert(found);

                        bestBranch.SimultaneousToClose.Add(keyForThisSimultaneous);
                    }
                    else if (current is IExecutableJoin join)
                    {
                        var simKey = bestBranch.SimultaneousToClose[^1];
                        int concurrentPaths = 0;
                        foreach (var path in branches)
                        {
                            if (path.SimultaneousToClose.Contains(simKey))
                                concurrentPaths++;
                        }

                        Debug.Assert(concurrentPaths != 0);

                        if (concurrentPaths >= 2)
                        {
                            // Another path will reach this join,
                            // we can exit safely knowing that it will take over in our stead
                            branches.Remove(bestBranch);
                            return false;
                        }
                        else
                        {
                            // we're the last path to reach the join for this simultaneous, we'll take over
                            bestBranch.SimultaneousToClose.RemoveAt(bestBranch.SimultaneousToClose.Count - 1);
                        }
                    }

                    return true;
                }
            }
        }


        /// <summary>
        /// If there is no direct paths, restore may pick a node that was moved from a later point to the
        /// closest to current point rather than one which was visited earlier than that node but hasn't moved
        /// </summary>
        [Flags]
        public enum RestoreBehavior
        {
            NoRestriction = 0,
            /// <summary>
            /// When an event we previously completed does not exist anymore
            /// </summary>
            /// <remarks>
            /// If this flag is not set, restore ignores that event, and continues on as if we never visited it. Nodes visited through this event are considered not visited.
            /// </remarks>
            EventNotFound = 1 << 0,
            /// <summary>
            /// When the path we've previously traversed contains a node that now lay outside the path in the latest version
            /// </summary>
            /// <remarks>
            /// If this flag is not set, restore ignores that node, as if we never visited it
            /// </remarks>
            NodeNotInGraph = 1 << 1,
            /// <summary>
            /// When the path we're restoring from the previous state goes through a node the state hasn't been visited previously
            /// </summary>
            /// <remarks>
            /// This is most likely caused by a node having been inserted into an existing path in the latest version of the graph.
            /// If this flag is not set, restore adds this node to the path, as if the previous version already visited it
            /// </remarks>
            NodeNotInState = 1 << 2,
        }

        private class PathWalker
        {
            public readonly List<object> SimultaneousToClose;
            public IExecutable Current { get; private set; }

            public PathWalker(List<object> s, IExecutable Current)
            {
                SimultaneousToClose = s;
                this.Current = Current;
            }

            public void SetCurrent(IExecutable next, List<Link> progress)
            {
                progress.Add(new(Current, next));
                Current = next;
            }
        }
    }
}
