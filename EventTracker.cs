using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Screenplay.Nodes;

namespace Screenplay
{
    public class EventTracker : IEventTracker
    {
        private readonly AutoResetUniTaskCompletionSource _utcs;
        private readonly List<VisitedPermutation> _writer;
        private readonly Event _writeValue;
        private bool _isUnlocked;

        public EventRunnerState RunnerState { get; }

        public EventTracker(List<VisitedPermutation> sharedWriteTarget, AutoResetUniTaskCompletionSource utcs, Event writeValue, EventRunnerState runnerState)
        {
            _writer = sharedWriteTarget;
            _writeValue = writeValue;
            _utcs = utcs;
            RunnerState = runnerState;
        }

        public void SetUnlockedState(bool state, params (VariantBase, guid)[] permutationValues)
        {
            lock (_writer)
            {
                if (_isUnlocked == state)
                    return;

                _isUnlocked = !_isUnlocked;
                if (_isUnlocked)
                {
                    _writer.Add(new VisitedPermutation{ Event = _writeValue, Variants = permutationValues });
                    _utcs.TrySetResult();
                }
                else
                {
                    for (int i = _writer.Count - 1; i >= 0; i--)
                    {
                        if (_writer[i].Event == _writeValue)
                        {
                            _writer.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }
    }
}
