using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Screenplay.Nodes;

namespace Screenplay
{
    public class Door
    {
        private readonly IEventTracker _parentTracker;
        private readonly AutoResetUniTaskCompletionSource _autoReset = AutoResetUniTaskCompletionSource.Create();
        private readonly Dictionary<Latch, (VariantBase, guid)[]> _permutations = new();
        private readonly object _lock = new();
        private int _counter;

        public bool Closed => _counter == 0;

        public UniTask WaitOpen() => Closed ? _autoReset.Task : UniTask.CompletedTask;

        public UniTask WaitClosed() => Closed ? UniTask.CompletedTask : _autoReset.Task;

        public (VariantBase, guid)[] CollectPermutations()
        {
            lock (_lock)
            {
                var list = new List<(VariantBase, guid)>();
                foreach (var (latch, permutation) in _permutations)
                    list.AddRange(permutation);

                return list.ToArray();
            }
        }

        public Door(IEventTracker parentTracker)
        {
            _parentTracker = parentTracker;
        }

        public class Latch : IEventTracker
        {
            private readonly Door _door;
            private bool _isUnlocked;

            public EventRunnerState RunnerState => _door._parentTracker.RunnerState;

            public void SetUnlockedState(bool state, params (VariantBase, guid)[] permutationValues)
            {
                if (_isUnlocked == state)
                    return;

                _isUnlocked = state;

                lock (_door._lock)
                {
                    if (_isUnlocked)
                        _door._permutations[this] = permutationValues;
                    else
                        _door._permutations.Remove(this);

                    if (   _isUnlocked          && --_door._counter == 0
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
