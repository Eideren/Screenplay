using System;
using System.Collections.Generic;

namespace Screenplay
{
    public class PreconditionCollector : IPreconditionCollector
    {
        private readonly List<GlobalId> _lastAppliedLocals = new();
        private readonly Action<PreconditionCollector> _onUnlocked;
        private readonly Action<PreconditionCollector> _onLocked;

        public bool IsUnlocked { get; private set; }

        public ScreenplayGraph.Introspection Introspection { get; }

        public Locals SharedLocals { get; } = new();
        public LatentVariable<bool> IsBusy { get; }

        public PreconditionCollector(Action<PreconditionCollector> onUnlocked, Action<PreconditionCollector> onLocked, LatentVariable<bool> isBusy, Precondition target, ScreenplayGraph.Introspection introspection)
        {
            _onUnlocked = onUnlocked;
            _onLocked = onLocked;
            IsBusy = isBusy;
            Introspection = introspection;

            if (introspection.Preconditions.TryGetValue(target, out var list) == false)
                introspection.Preconditions[target] = list = new();
            list.Add(this);
        }

        public void SetUnlockedState(bool state, params GlobalId[] locals)
        {
            lock (SharedLocals)
            {
                if (IsUnlocked == state)
                    return;

                IsUnlocked = !IsUnlocked;
                if (IsUnlocked)
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
