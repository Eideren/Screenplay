using System;

namespace Screenplay.Variables
{
    public abstract class ComponentNumber<T> : ComponentVariable<T>, INumber where T : IComparable<T>, IConvertible
    {
        public decimal GetNumber() => Value.ToDecimal(null);
    }
}