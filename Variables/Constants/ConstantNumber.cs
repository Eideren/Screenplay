using System;

namespace Screenplay.Variables.Constants
{
    [Serializable] public abstract class ConstantNumber<T> : Constant<T>, INumber where T : IComparable<T>, IConvertible
    {
        public decimal GetNumber() => Value.ToDecimal(null);
    }
}