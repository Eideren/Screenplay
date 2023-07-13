using System;
using System.Collections.Generic;
using UnityEngine;

namespace Screenplay.Variables
{
    public abstract class AssetVariable<T> : ScriptableObject, IVariable<T> where T : IComparable<T>
    {
        public T Value;
        [Multiline] public string Note;
        [SerializeField, HideInInspector] string _cachedName;

        public void Awake()
        {
            _cachedName = name;
        }

        public void OnValidate()
        {
            _cachedName = name;
        }

        T IVariable<T>.Value
        {
            get => Value;
            set => Value = value;
        }

        public void ValidateSelf() { }
        public IEnumerable<(string, IValidatable)> GetSubValues() => this.NoSubValues();
        public string GetInspectorString() => _cachedName;

        public void SetValue(T newValue)
        {
            Value = newValue;
        }

        public override string ToString()
        {
            string cachedName = _cachedName;
            if (string.IsNullOrEmpty(cachedName))
                _cachedName = name;
            return _cachedName;
        }
    }
}