using System;
using Screenplay.Nodes;

namespace Screenplay
{
    public struct VisitedPermutation : IEquatable<VisitedPermutation>
    {
        private (VariantBase, guid)[] _variants;
        public required Event Event;

        public required (VariantBase, guid)[] Variants
        {
            get
            {
                return _variants;
            }
            set
            {
                guid previous = default;
                foreach (var (variant, guid)  in value)
                {
                    if (previous.CompareTo(guid) < 0)
                        throw new ArgumentException();
                    previous = guid;
                }
                _variants = value;
            }
        }

        public bool Equals(VisitedPermutation other) => Event.Equals(other.Event) && Variants.AsSpan().SequenceEqual(other.Variants);

        public override bool Equals(object? obj) => obj is VisitedPermutation other && Equals(other);

        public override int GetHashCode()
        {
            var h = new HashCode();
            h.Add(Event);
            foreach (var (variant, guid) in Variants)
            {
                h.Add(variant);
                h.Add(guid);
            }
            return h.ToHashCode();
        }
    }
}
