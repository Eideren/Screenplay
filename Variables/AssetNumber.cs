using System;

namespace Screenplay.Variables
{
    public abstract class AssetNumber<T> : AssetVariable<T>, INumber where T : IComparable<T>, IConvertible
    {
        public decimal GetNumber() => Value.ToDecimal(null);
    }
}