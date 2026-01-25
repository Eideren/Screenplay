using System;
using UnityEngine;
using Event = Screenplay.Nodes.Event;

namespace Screenplay
{
    [Serializable]
    public struct VisitedPermutation : IEquatable<VisitedPermutation>
    {
        [SerializeField] private GlobalId[] _local;
        [SerializeReference] public required Event Event;

        public required GlobalId[] Local
        {
            get
            {
                return _local;
            }
            set
            {
                _local = new GlobalId[value.Length];
                Array.Copy(value, _local, value.Length);
                Array.Sort(_local, Comparison);

                int Comparison(GlobalId x, GlobalId y)
                {
                    int localComp = x.DeclarerGuid.CompareTo(y.DeclarerGuid);
                    return localComp != 0 ? localComp : x.ValueGuid.CompareTo(y.ValueGuid);
                }
            }
        }

        public bool Equals(VisitedPermutation other) => Event.Equals(other.Event) && Local.AsSpan().SequenceEqual(other.Local);

        public override bool Equals(object? obj) => obj is VisitedPermutation other && Equals(other);

        public override int GetHashCode()
        {
            var h = new HashCode();
            h.Add(Event);
            foreach (var global in Local)
            {
                h.Add(global.DeclarerGuid);
                h.Add(global.ValueGuid);
            }
            return h.ToHashCode();
        }
    }
}
