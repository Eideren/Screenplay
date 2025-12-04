using System;
using Screenplay.Nodes;

namespace Screenplay
{
    public struct VisitedPermutation : IEquatable<VisitedPermutation>
    {
        private (ILocal, guid)[] _local;
        public required Event Event;

        public required (ILocal, guid)[] Local
        {
            get
            {
                return _local;
            }
            set
            {
                _local = new (ILocal, guid)[value.Length];
                Array.Copy(value, _local, value.Length);
                Array.Sort(_local, Comparison);

                int Comparison((ILocal local, guid value) x, (ILocal local, guid value) y)
                {
                    int localComp = x.local.Id.CompareTo(y.local.Id);
                    return localComp != 0 ? localComp : x.value.CompareTo(y.value);
                }
            }
        }

        public bool Equals(VisitedPermutation other) => Event.Equals(other.Event) && Local.AsSpan().SequenceEqual(other.Local);

        public override bool Equals(object? obj) => obj is VisitedPermutation other && Equals(other);

        public override int GetHashCode()
        {
            var h = new HashCode();
            h.Add(Event);
            foreach (var (variant, guid) in Local)
            {
                h.Add(variant);
                h.Add(guid);
            }
            return h.ToHashCode();
        }
    }
}
