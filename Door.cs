using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Screenplay
{
    public class Door
    {
        private readonly IPreconditionCollector _parentTracker;
        private readonly object _lock = new();
        private readonly CancelableAutoResetEvent<int> _openEvent = new(), _closeEvent = new();
        private int _counter;

        public bool Open => _counter == 0;

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

        public Door(IPreconditionCollector parentTracker, IList<Precondition> targets, out IPreconditionCollector[] preconditions)
        {
            _parentTracker = parentTracker;
            preconditions = new IPreconditionCollector[targets.Count];
            for (int i = 0; i < preconditions.Length; i++)
                preconditions[i] = new Latch(this, targets[i]);
        }

        private class Latch : IPreconditionCollector
        {
            private readonly List<GlobalId> _lastAppliedLocals = new();
            private readonly Door _door;

            public bool IsUnlocked { get; private set; }

            public ScreenplayGraph.Introspection Introspection => _door._parentTracker.Introspection;

            public Locals SharedLocals => _door._parentTracker.SharedLocals;

            public void SetUnlockedState(bool state, params GlobalId[] locals)
            {
                if (IsUnlocked == state)
                    return;

                IsUnlocked = state;

                lock (_door._lock)
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

                    if (IsUnlocked             && --_door._counter == 0
                        || IsUnlocked == false && ++_door._counter == 1)
                    {
                        if (_door.Open)
                            _door._openEvent.Signal(0);
                        else
                            _door._closeEvent.Signal(0);
                    }
                }
            }

            public Latch(Door door, Precondition target)
            {
                _door = door;
                _door._counter++;

                if (Introspection.Preconditions.TryGetValue(target, out var list) == false)
                    Introspection.Preconditions[target] = list = new();
                list.Add(this);
            }
        }
    }
}
