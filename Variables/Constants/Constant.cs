using System;
using System.Collections.Generic;

namespace Screenplay.Variables.Constants
{
    [Serializable] public abstract class Constant<T> : IValue<T> where T : IComparable<T>
    {
        public T Value;
        T IValue<T>.Value => Value;
        public void ValidateSelf() { }
        public IEnumerable<(string, IValidatable)> GetSubValues() => this.NoSubValues();
        public string GetInspectorString() => Value.ToString();
    }
}