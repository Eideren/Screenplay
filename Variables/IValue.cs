using System;
using SerializeReferenceUI;

namespace Screenplay.Variables
{
    public interface IValue : IValidatable, IInspectorString
    {
        bool TryCompare(IValue other, out int compResult);
        string EvalString(string format = null, IFormatProvider formatProvider = null);
    }

    public interface IValue<T> : IValue where T : IComparable<T>
    {
        T Value { get; }
        bool IValue.TryCompare(IValue other, out int compResult) => DefaultTryCompare(this, other, out compResult);

        string IValue.EvalString(string format, IFormatProvider formatProvider)
        {
            if ((object)Value is IFormattable formattableValue)
                return formattableValue.ToString(format, formatProvider);
            return Value.ToString();
        }

        static bool DefaultTryCompare(IValue<T> @this, IValue other, out int compResult)
        {
            if (other is IValue<T> otherAsT)
            {
                compResult = @this.Value.CompareTo(otherAsT.Value);
                return true;
            }

            if (@this is INumber thisN && other is INumber otherN)
            {
                compResult = thisN.GetNumber().CompareTo(otherN.GetNumber());
                return true;
            }

            compResult = 0;
            return false;
        }
    }
}