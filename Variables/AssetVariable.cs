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
        [NonSerialized] T _rollbackValue;

        public void Awake()
        {
            _cachedName = name;
        }

        public void OnEnable()
        {
            #if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                SetupRollback();
            #else
            SetupRollback();
            #endif

            void SetupRollback()
            {
                _rollbackValue = Value;
                AssetVariableManagement.RollbackVariables += () => Value = _rollbackValue;
            }
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

        /// <summary> Same thing as setting <see cref="Value"/> </summary>
        public void SetValue(T newValue) => Value = newValue;

        public override string ToString()
        {
            string cachedName = _cachedName;
            if (string.IsNullOrEmpty(cachedName))
                _cachedName = name;
            return _cachedName;
        }
    }

    public static class AssetVariableManagement
    {
        /// <summary> Call to rollback all <see cref="AssetVariable{T}"/> to their initial values </summary>
        public static Action RollbackVariables;

#if UNITY_EDITOR
        static AssetVariableManagement()
        {
            // Scriptable object persist changes across session, we don't want that for variables so we're manually resetting them
            UnityEditor.EditorApplication.playModeStateChanged += change =>
            {
                if(change != UnityEditor.PlayModeStateChange.ExitingPlayMode)
                    return;
                RollbackVariables?.Invoke();
            };
        }
#endif
    }
}