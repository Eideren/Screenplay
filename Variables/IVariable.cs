using System;

namespace Screenplay.Variables
{
    public interface IVariable : IValue
    {
        bool CanBeSetTo(IValue val);
        void SetTo(IValue val);
        bool CanParse(string str);
        void SetFromParsedString(string str);
    }

    public interface IVariable<T> : IVariable, IValue<T> where T : IComparable<T>
    {
        new T Value { get; set; }
        T IValue<T>.Value => Value;

        bool IVariable.CanBeSetTo(IValue val) => Setter(val, true);
        bool IVariable.CanParse(string str) => Parser(str, true);


        void IVariable.SetTo(IValue val)
        {
            if (Setter(val, false) == false)
                throw new InvalidOperationException($"Incompatible setter between recipient '{this}' and value '{val}'");
        }


        void IVariable.SetFromParsedString(string str)
        {
            if (Parser(str, false) == false)
                throw new InvalidOperationException($"Incompatible setter between recipient '{this}' and value '{str}'");
        }


        bool Setter(IValue valueSource, bool test)
        {
            if (valueSource is IValue<T> sourceIsT)
            {
                if (test == false)
                    Value = sourceIsT.Value;
                return true;
            }

            if (valueSource is not INumber number)
                return false;

            if (test)
            {
                switch (Type.GetTypeCode(Value.GetType()))
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
            switch (this)
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
                default: throw new NotImplementedException(GetType().ToString());
            }

            return true;
        }

        bool Parser(string value, bool test)
        {
            if (this is IVariable<string> stringVar)
            {
                if (test == false)
                    stringVar.Value = value;
                return true;
            }

            try
            {
                switch (this)
                {
                    case IVariable<bool> recipient2: recipient2.Value = NoOpTests(recipient2.Value, Convert.ToBoolean(value), test); break;
                    case IVariable<Int16> recipient2: recipient2.Value = NoOpTests(recipient2.Value, Convert.ToInt16(value), test); break;
                    case IVariable<Int32> recipient2: recipient2.Value = NoOpTests(recipient2.Value, Convert.ToInt32(value), test); break;
                    case IVariable<Int64> recipient2: recipient2.Value = NoOpTests(recipient2.Value, Convert.ToInt64(value), test); break;
                    case IVariable<UInt16> recipient2: recipient2.Value = NoOpTests(recipient2.Value, Convert.ToUInt16(value), test); break;
                    case IVariable<UInt32> recipient2: recipient2.Value = NoOpTests(recipient2.Value, Convert.ToUInt32(value), test); break;
                    case IVariable<UInt64> recipient2: recipient2.Value = NoOpTests(recipient2.Value, Convert.ToUInt64(value), test); break;
                    case IVariable<float> recipient2: recipient2.Value = NoOpTests(recipient2.Value, Convert.ToSingle(value), test); break;
                    case IVariable<double> recipient2: recipient2.Value = NoOpTests(recipient2.Value, Convert.ToDouble(value), test); break;
                    case IVariable<decimal> recipient2: recipient2.Value = NoOpTests(recipient2.Value, Convert.ToDecimal(value), test); break;
                    default: throw new NotImplementedException(GetType().ToString());
                }
            }
            catch (FormatException)
            {
                return false;
            }

            return true;

            // We want to evaluate convert to see if it throws, but we do not want to assign it when testing
            // There are of course more optimized ways to do this but they are also more wordy and I don't think optimizing this context matters
            static T2 NoOpTests<T2>(T2 original, T2 newV, bool isTest) => isTest ? original : newV;
        }
    }
}