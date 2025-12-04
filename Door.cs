using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Screenplay.Nodes;

namespace Screenplay
{
    public class Door
    {
        private readonly IPreconditionCollector _parentTracker;
        private readonly AutoResetUniTaskCompletionSource _autoReset = AutoResetUniTaskCompletionSource.Create();
        private readonly object _lock = new();
        private int _counter;

        public bool Closed => _counter == 0;

        public UniTask WaitOpen() => Closed ? _autoReset.Task : UniTask.CompletedTask;

        public UniTask WaitClosed() => Closed ? UniTask.CompletedTask : _autoReset.Task;

        public Door(IPreconditionCollector parentTracker)
        {
            _parentTracker = parentTracker;
        }

        public class Latch : IPreconditionCollector
        {
            private readonly List<(ILocal, guid)> _lastAppliedVariants = new();
            private readonly Door _door;
            private bool _isUnlocked;

            public Locals SharedLocals => _door._parentTracker.SharedLocals;

            public LatentVariable<bool> IsBusy => _door._parentTracker.IsBusy;

            public void SetUnlockedState(bool state, params (ILocal, guid)[] locals)
            {
                if (_isUnlocked == state)
                    return;

                _isUnlocked = state;

                lock (_door._lock)
                {
                    if (_isUnlocked)
                    {
                        foreach (var v in locals)
                        {
                            if (SharedLocals.TryAdd(v))
                                _lastAppliedVariants.Add(v);
                        }
                    }
                    else
                    {
                        foreach (var v in _lastAppliedVariants)
                            SharedLocals.Remove(v);
                        _lastAppliedVariants.Clear();
                    }

                    if (_isUnlocked             && --_door._counter == 0
                        || _isUnlocked == false && ++_door._counter == 1)
                    {
                        _door._autoReset.TrySetResult();
                    }
                }
            }

            public Latch(Door door)
            {
                _door = door;
                _door._counter++;
            }
        }
    }
}
