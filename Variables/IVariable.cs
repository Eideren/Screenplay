using System;

namespace Screenplay.Variables
{
    public interface IVariable : IValue
    {
        bool CanBeSetTo(IValue val, out Action<IVariable, IValue> setter);
    }

    public interface IVariable<T> : IVariable, IValue<T> where T : IComparable<T>
    {
        static Action<IVariable, IValue> DirectTypeSetter;
        static Action<IVariable, IValue> NumberSetter;

        static IVariable()
        {
            DirectTypeSetter = DirectType;
            NumberSetter = Number;
        }

        new T Value { get; set; }
        T IValue<T>.Value => Value;
        bool IVariable.CanBeSetTo(IValue val, out Action<IVariable, IValue> setter) => DefaultCanBeSetTo(this, val, out setter);

        static bool DefaultCanBeSetTo(IVariable<T> @this, IValue val, out Action<IVariable, IValue> setter)
        {
            if (val is IValue<T>)
            {
                setter = DirectTypeSetter;
                return true;
            }

            if (val is INumber)
            {
                if (@this is IVariable<bool> || @this is IVariable<int> || @this is IVariable<float> || @this is IVariable<double>)
                {
                    setter = NumberSetter;
                    return true;
                }
            }

            setter = null;
            return false;
        }

        static void DirectType(IVariable arg1, IValue arg2)
        {
            ((IVariable<T>)arg1).Value = ((IValue<T>)arg2).Value;
        }

        static void Number(IVariable arg1, IValue arg2)
        {
            if (arg1 is not IVariable<bool> a4)
            {
                if (arg1 is not IVariable<int> a3)
                {
                    if (arg1 is not IVariable<float> a2)
                    {
                        if (arg1 is not IVariable<double> a)
                            throw new InvalidOperationException($"{arg1.GetType()} is not a valid type");
                        a.Value = ((INumber)arg2).GetNumber();
                    }
                    else
                        a2.Value = (float)((INumber)arg2).GetNumber();
                }
                else
                    a3.Value = (int)((INumber)arg2).GetNumber();
            }
            else
                a4.Value = ((INumber)arg2).GetNumber() > 0.0;
        }
    }
}