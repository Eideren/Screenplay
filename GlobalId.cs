using System;

namespace Screenplay
{
    /// <summary>
    /// An Id to a specific global value defined in a <see cref="Screenplay"/> which can be serialized and restored
    /// for persistance and saving purposes
    /// </summary>
    public readonly struct GlobalId : IEquatable<GlobalId>
    {
        /// <summary> The ID for a <see cref="IUntypedGlobalsDeclarer"/> </summary>
        public readonly guid DeclarerGuid;

        /// <summary>
        /// The ID for a value declared by a <see cref="IUntypedGlobalsDeclarer"/>
        /// which can be used to retrieve the actual value through <see cref="IUntypedGlobalsDeclarer.TryGetValue"/>
        /// </summary>
        public readonly guid ValueGuid;

        public GlobalId(guid declarerGuid, guid valueGuid)
        {
            DeclarerGuid = declarerGuid;
            ValueGuid = valueGuid;
        }

        public bool Equals(GlobalId other) => DeclarerGuid.Equals(other.DeclarerGuid) && ValueGuid.Equals(other.ValueGuid);

        public override bool Equals(object? obj) => obj is GlobalId other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(DeclarerGuid, ValueGuid);
    }
}
