using System;
using System.Collections.Generic;

namespace Screenplay
{
    public class PreconditionCollector : IPreconditionCollector
    {
        private readonly List<GlobalId> _lastAppliedLocals = new();
        private readonly Action<PreconditionCollector> _onUnlocked;
        private readonly Action<PreconditionCollector> _onLocked;
        private bool _isUnlocked;

        public Locals SharedLocals { get; } = new();
        public LatentVariable<bool> IsBusy { get; }

        public PreconditionCollector(Action<PreconditionCollector> onUnlocked, Action<PreconditionCollector> onLocked, LatentVariable<bool> isBusy)
        {
            _onUnlocked = onUnlocked;
            _onLocked = onLocked;
            IsBusy = isBusy;
        }

        public void SetUnlockedState(bool state, params GlobalId[] locals)
        {
            lock (SharedLocals)
            {
                if (_isUnlocked == state)
                    return;

                _isUnlocked = !_isUnlocked;
                if (_isUnlocked)
                {
                    foreach (var v in locals)
                    {
                        if (SharedLocals.TryAdd(v))
                            _lastAppliedLocals.Add(v);
                    }

                    _onUnlocked(this);
                }
                else
                {
                    foreach (var v in _lastAppliedLocals)
                        SharedLocals.Remove(v);
                    _lastAppliedLocals.Clear();

                    _onLocked(this);
                }
            }
        }
    }
}
