using System;
using System.Collections.Generic;

namespace Screenplay.Variables
{
    public sealed class VariableRef : IVariable
    {
        public UInterface<IVariable> Variable;
        public bool TryCompare(IValue other, out int compResult) => Variable.Reference.TryCompare(other, out compResult);
        public string EvalString(string format = null, IFormatProvider formatProvider = null) => Variable.Reference.EvalString(format, formatProvider);
        public string GetInspectorString() => Variable.Reference != null ? Variable.Reference.GetInspectorString() : "null";

        public void ValidateSelf()
        {
            if (Variable.Reference == null)
                throw new NullReferenceException(nameof(Variable));
        }

        public IEnumerable<(string, IValidatable)> GetSubValues()
        {
            yield return (nameof(Variable), Variable.Reference);
        }

        public bool CanBeSetTo(IValue val) => Variable.Reference.CanBeSetTo(val);

        public void SetTo(IValue val) => Variable.Reference.SetTo(val);
    }
}