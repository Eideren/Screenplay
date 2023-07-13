using System.Collections.Generic;

namespace Screenplay.Variables
{
    public abstract class BinaryOperator : IBool
    {
        public abstract bool Value { get; }
        public abstract void ValidateSelf();
        public abstract IEnumerable<(string, IValidatable)> GetSubValues();
        public abstract string GetInspectorString();
    }
}