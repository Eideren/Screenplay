using System;

namespace Screenplay.Variables
{
    public interface IVariable : IValue
    {
        bool CanBeSetTo(IValue val);
        void SetTo(IValue val);
    }

    public interface IVariable<T> : IVariable, IValue<T> where T : IComparable<T>
    {
        new T Value { get; set; }
        T IValue<T>.Value => Value;

        bool IVariable.CanBeSetTo(IValue val) => Setter(this, val, true);

        void IVariable.SetTo(IValue val)
        {
            if (Setter(this, val, false) == false)
                throw new InvalidOperationException($"Incompatible setter between recipient '{this}' and value '{val}'");
        }


        bool Setter(IVariable<T> recipient, IValue valueSource, bool test)
        {
            if (valueSource is IValue<T> sourceIsT)
            {
                if (test == false)
                    recipient.Value = sourceIsT.Value;
                return true;
            }

            if (valueSource is not INumber number)
                return false;

            if (test)
            {
                switch (Type.GetTypeCode(recipient.Value.GetType()))
                {
                    case TypeCode.Boolean:
                    case TypeCode.Byte:
                    case TypeCode.Decimal:
                    case TypeCode.Double:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.SByte:
                    case TypeCode.Single:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        return true;
                    case TypeCode.String:
                    case TypeCode.Object:
                    case TypeCode.Empty:
                    case TypeCode.DBNull:
                    case TypeCode.DateTime:
                    case TypeCode.Char:
                    default:
                        return false;
                }
            }

            IConvertible convertible = number.GetNumber();
            switch (recipient)
            {
                case IVariable<bool> recipient2: recipient2.Value = convertible.ToBoolean(null); break;
                case IVariable<Int16> recipient2: recipient2.Value = convertible.ToInt16(null); break;
                case IVariable<Int32> recipient2: recipient2.Value = convertible.ToInt32(null); break;
                case IVariable<Int64> recipient2: recipient2.Value = convertible.ToInt64(null); break;
                case IVariable<UInt16> recipient2: recipient2.Value = convertible.ToUInt16(null); break;
                case IVariable<UInt32> recipient2: recipient2.Value = convertible.ToUInt32(null); break;
                case IVariable<UInt64> recipient2: recipient2.Value = convertible.ToUInt64(null); break;
                case IVariable<float> recipient2: recipient2.Value = convertible.ToSingle(null); break;
                case IVariable<double> recipient2: recipient2.Value = convertible.ToDouble(null); break;
                case IVariable<decimal> recipient2: recipient2.Value = convertible.ToDecimal(null); break;
                default: throw new NotImplementedException(recipient.GetType().ToString());
            }

            return true;
        }
    }
}