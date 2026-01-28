using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Screenplay
{
    public class Door
    {
        private readonly IPreconditionCollector _parentTracker;
        private readonly AutoResetUniTaskCompletionSource _autoReset = AutoResetUniTaskCompletionSource.Create();
        private readonly object _lock = new();
        private int _counter;

        public bool Open => _counter == 0;

        public UniTask WaitOpen() => Open ? UniTask.CompletedTask : _autoReset.Task;

        public UniTask WaitClosed() => Open ? _autoReset.Task : UniTask.CompletedTask;

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

            public LatentVariable<bool> IsBusy => _door._parentTracker.IsBusy;

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
                        _door._autoReset.TrySetResult();
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
