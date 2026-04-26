using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Screenplay
{
    public class Lock
    {
        private readonly IPreconditionCollector _parentTracker;
        // Need to split into two as a close might signal an open and vice versa
        private readonly CancelableAutoResetEvent<int> _openEvent = new(), _closeEvent = new();
        private readonly object _lock = new();
        private int _counter;

        public bool Open => _counter != 0;

        public async UniTask WaitOpen(Cancellation cancellation)
        {
            if (Open)
                return;
            await _openEvent.NextSignal(cancellation);
        }

        public async UniTask WaitClosed(Cancellation cancellation)
        {
            if (Open == false)
                return;
            await _closeEvent.NextSignal(cancellation);
        }

        public Lock(IPreconditionCollector parentTracker, IList<Precondition> targets, out IPreconditionCollector[] preconditions)
        {
            _parentTracker = parentTracker;
            preconditions = new IPreconditionCollector[targets.Count];
            for (int i = 0; i < preconditions.Length; i++)
                preconditions[i] = new Key(this, targets[i]);
        }

        private class Key : IPreconditionCollector
        {
            private readonly List<GlobalId> _lastAppliedLocals = new();
            private readonly Lock _lock;

            public bool IsUnlocked { get; private set; }

            public ScreenplayGraph.Introspection Introspection => _lock._parentTracker.Introspection;

            public Locals SharedLocals => _lock._parentTracker.SharedLocals;

            public void SetUnlockedState(bool state, params GlobalId[] locals)
            {
                if (IsUnlocked == state)
                    return;

                IsUnlocked = state;

                lock (_lock._lock)
                {
                    if (IsUnlocked)
                    {
                        foreach (var v in locals)
                        {
                            if (SharedLocals.TryAdd(v))
                                _lastAppliedLocals.Add(v);
                        }
                    }
                    else
                    {
                        foreach (var v in _lastAppliedLocals)
                            SharedLocals.Remove(v);
                        _lastAppliedLocals.Clear();
                    }

                    int previousCounter = _lock._counter;
                    _lock._counter += IsUnlocked ? 1 : -1;

                    if (previousCounter == 0 && _lock._counter == 1
                        || previousCounter == 1 && _lock._counter == 0)
                    {
                        if (_lock.Open)
                            _lock._openEvent.Signal(0);
                        else
                            _lock._closeEvent.Signal(0);
                    }
                }
            }

            public Key(Lock l, Precondition target)
            {
                _lock = l;

                if (Introspection.Preconditions.TryGetValue(target, out var list) == false)
                    Introspection.Preconditions[target] = list = new();
                list.Add(this);
            }
        }
    }
}
