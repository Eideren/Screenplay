using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes.Barriers
{
    public interface IBarrierPart : INodeValue
    {
        public static bool InNodeEditor;

        IPort[] InheritedPorts { get; }
        IBarrierPart? NextBarrier { get; set; }
        IEnumerable<IOutput> AllTracks();

#warning hookup this method, must run whenever the amount of output changes, or whenever a different barrier is connected
        void UpdatePorts(IBarrierPart parent);

        public class Group : IDisposable
        {
            private static readonly Dictionary<CancellationToken, Group> s_branchToGroup = new();

            private readonly List<IBarrierPart> _barrierChain = new();
            private readonly List<(BarrierIntermediate barrier, CancellationTokenSource tokenSource)> _loopDestination = new();
            private readonly List<CancellationTokenSource> _runningLinears = new();
            private int _owningThreadId;
            private int _previousBarrierIndex = 0;
            private IBarrierPart? _continuation;
            private IEventContext _entryContext;
            private CancellationToken _initialTokenSource;
            private Signal<BarrierEnd> _continuationSignal;
            private long _disposed;

            public Group(IEventContext context, CancellationToken initialTokenSource, Barrier entryPoint, out UniTask<BarrierEnd> completion)
            {
                _owningThreadId = Thread.CurrentThread.ManagedThreadId;
                _entryContext = context;
                _initialTokenSource = initialTokenSource;
                _continuationSignal = new Signal<BarrierEnd>();

                for (IBarrierPart? part = entryPoint; part != null; part = part.NextBarrier)
                    _barrierChain.Add(part);

                completion = _continuationSignal.NewTask(initialTokenSource, cancelImmediately:true);

                AddLoops(entryPoint);
                AddLinears(entryPoint);
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                    return;

                lock (_runningLinears)
                {
                    lock (s_branchToGroup)
                    {
                        foreach (var runningLinear in _runningLinears)
                            s_branchToGroup.Remove(runningLinear.Token);
                    }

                    for (int i = _runningLinears.Count - 1; i >= 0; i--)
                        _runningLinears[i].Cancel(); // dispose is taken care of in the loop's execution
                    Debug.Assert(_runningLinears.Count == 0);
                }

                lock (_loopDestination)
                {
                    for (int i = _loopDestination.Count - 1; i >= 0; i--)
                        _loopDestination[i].tokenSource.Cancel(); // dispose is taken care of in the loop's execution
                    Debug.Assert(_loopDestination.Count == 0);
                }

                _continuationSignal.CancelWaitingSignals();
                _continuationSignal.Dispose();
            }

            public static void NotifyReceivedGroup(CancellationToken tokenId, IBarrierPart receiver)
            {
                Group group;
                lock (s_branchToGroup)
                    group = s_branchToGroup[tokenId]; // No TryGet, it would be an error for this dictionary to not have this id

                group.NotifReceivedGroup(tokenId, receiver);
            }

            private void AddLoops(IBarrierPart part)
            {
                var cts = new List<(CancellationTokenSource cts, IOutput branch)>();
                foreach (var track in part.AllTracks())
                {
                    if (track.LoopsWithin is { } destination)
                    {
                        var tcs = CancellationTokenSource.CreateLinkedTokenSource(_initialTokenSource);
                        lock (s_branchToGroup)
                            s_branchToGroup[tcs.Token] = this;
                        lock (_loopDestination)
                            _loopDestination.Add((destination, tcs));
                        cts.Add((tcs, track));
                    }
                }

                foreach (var (ct, track) in cts)
                    AddLoop(track, ct);
            }

            private void AddLinears(IBarrierPart part)
            {
                // Must first populate the arrays before we start the linears to ensure that it doesn't exit
                // early when the first one runs and finishes right away
                var cts = new List<(CancellationTokenSource cts, IOutput branch)>();
                foreach (var track in part.AllTracks())
                {
                    if (track.LoopsWithin is null)
                    {
                        var tcs = CancellationTokenSource.CreateLinkedTokenSource(_initialTokenSource);
                        lock (s_branchToGroup)
                            s_branchToGroup[tcs.Token] = this;
                        lock (_loopDestination)
                            _runningLinears.Add(tcs);
                        cts.Add((tcs, track));
                    }
                }

                foreach (var (ct, track) in cts)
                    AddLinear(track, ct);
            }

            private async void AddLoop(IOutput loopOutput, CancellationTokenSource cts)
            {
                try
                {
                    if (loopOutput.LoopsWithin is null)
                        throw new NullReferenceException(nameof(loopOutput.LoopsWithin));

                    do
                    {
                        await loopOutput.Execute(_entryContext, cts.Token);
                    } while (cts.IsCancellationRequested == false);
                }
                catch (OperationCanceledException) { }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    lock (_loopDestination)
                    {
                        for (int i = _loopDestination.Count - 1; i >= 0; i--)
                        {
                            if (_loopDestination[i].tokenSource == cts)
                            {
                                _loopDestination.RemoveAt(i);
                                break;
                            }
                        }
                    }
                    cts.Dispose();
                }
            }

            private async void AddLinear(IOutput loopOutput, CancellationTokenSource cts)
            {
                try
                {
                    if (loopOutput.LoopsWithin is not null)
                        throw new NullReferenceException(nameof(loopOutput.LoopsWithin));

                    await loopOutput.Execute(_entryContext, cts.Token);
                }
                catch (OperationCanceledException) { }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    lock (_runningLinears)
                        _runningLinears.Remove(cts);
                    cts.Dispose();
                }
            }

            private void NotifReceivedGroup(CancellationToken tokenId, IBarrierPart receiver)
            {
                if (_owningThreadId != Thread.CurrentThread.ManagedThreadId)
                    throw new Exception("Object accessed from a different thread than its constructor's, this is not allowed");

                bool linear = false;
                bool lastOne;
                lock (_runningLinears)
                {
                    if (Interlocked.Read(ref _disposed) != 0)
                        return;

                    for (int i = _runningLinears.Count - 1; i >= 0; i--)
                    {
                        var tokenSource = _runningLinears[i];
                        if (tokenSource.Token == tokenId)
                        {
                            _runningLinears.RemoveAt(i);
                            linear = true;
                        }
                    }

                    lastOne = _runningLinears.Count == 0;
                }

                if (linear == false)
                {
                    lock (_loopDestination)
                    {
                        foreach (var tuple in _loopDestination)
                        {
                            if (tuple.tokenSource.Token == tokenId)
                                return; // This is most likely a loop, we can just return and the loop should go again
                        }
                    }

                    throw new InvalidOperationException("Invalid token received, did you connect a node that started outside of a barrier into an intermediate barrier ?");
                }

                int indexOfReceiver = _barrierChain.IndexOf(receiver);
                if (_continuation == null || _barrierChain.IndexOf(_continuation) < indexOfReceiver)
                    _continuation = receiver;

                if (lastOne == false)
                    return; // Still waiting on another linear to come in ...

                if (_continuation is BarrierEnd barrierEnd)
                {
                    _continuationSignal.Send(barrierEnd);
                    return;
                }

                int indexOfThisBarrier = _barrierChain.IndexOf(_continuation);

                for (int i = _previousBarrierIndex+1, end = indexOfThisBarrier+1; i < end; i++)
                {
                    // Cancel loops that should have ended on a previous or on this node
                    var skippedBarrier = _barrierChain[i];
                    lock (_loopDestination)
                    {
                        for (int j = _loopDestination.Count - 1; j >= 0; j--)
                        {
                            var (barrier, cts) = _loopDestination[j];
                            if (barrier == skippedBarrier)
                                cts.Cancel();
                        }
                    }

                    AddLoops(skippedBarrier);
                }
                _previousBarrierIndex = indexOfThisBarrier;

                AddLinears(_continuation);
            }
        }
    }
}
