using System;
using Screenplay.Variables;
using TMPro;
using UnityEngine;

namespace Screenplay
{
    public class VariableDisplay : MonoBehaviour, IValueReadWriter
    {
        public TMP_Text Text;
        public UInterface<IVariable> Variable;
        object _variableCache;

        void Update() => Variable.Reference.ReadWrite(this);

        public void ReadWriteValue<T>(ref T value)
        {
            if (_variableCache is VariableCache<IEquatable<T>> cache)
            {
                if (cache.CacheValue.Equals(value) == false)
                {
                    cache.CacheValue = (IEquatable<T>)value;
                    Text.text = value.ToString();
                }

                return;
            }

            if (value is IEquatable<T> equatableValue)
            {
                _variableCache = new VariableCache<IEquatable<T>> { CacheValue = equatableValue };
                Text.text = value.ToString();
                return;
            }

            if (_variableCache is not T || (_variableCache is T t && t.Equals(value) == false))
            {
                _variableCache = value;
                Text.text = value.ToString();
                return;
            }
        }

        class VariableCache<T>
        {
            public T CacheValue;
        }
    }
}