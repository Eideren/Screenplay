using System;
using System.Collections.Generic;
using UnityEngine;

namespace Screenplay.Variables
{
    public abstract class ComponentVariable<T> : MonoBehaviour, IVariable<T> where T : IComparable<T>
    {
        public string Name = "Unnamed variable";
        public T Value;

        T IVariable<T>.Value
        {
            get => Value;
            set => Value = value;
        }

        public void ValidateSelf() { }
        public IEnumerable<(string, IValidatable)> GetSubValues() => this.NoSubValues();
        public string GetInspectorString() => Name;

        public void SetValue(T newValue)
        {
            Value = newValue;
        }

        public void ReadWrite(IValueReadWriter readWriter) => readWriter.ReadWriteValue(ref Value);

        public override string ToString() => string.IsNullOrEmpty(Name) ? base.ToString() : Name;
    }
}