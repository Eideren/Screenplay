using System;
using System.Collections.Generic;

namespace Screenplay.Variables
{
    public sealed class NumberRef : INumber
    {
        public UInterface<INumber> Variable;
        public string GetInspectorString() => Variable.Reference?.GetInspectorString() ?? "null";
        public double GetNumber() => Variable.Reference.GetNumber();
        public bool TryCompare(IValue other, out int compResult) => Variable.Reference.TryCompare(other, out compResult);
        public string EvalString(string format = null, IFormatProvider formatProvider = null) => Variable.Reference.EvalString(format, formatProvider);

        public IEnumerable<(string, IValidatable)> GetSubValues()
        {
            yield return (nameof(Variable), Variable.Reference);
        }

        public void ValidateSelf()
        {
            if (Variable.Reference == null)
                throw new NullReferenceException("Variable");
        }
    }
}