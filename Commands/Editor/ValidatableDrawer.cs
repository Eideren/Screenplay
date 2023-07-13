using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Screenplay.Commands
{
    public abstract class ValidatableDrawer : PropertyDrawer
    {
        static readonly Dictionary<IValidatable, (Validity validity, Exception exception)> _cache = new();

        public sealed override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            OnGUIValidatable(position, property, label);
            if (EditorGUI.EndChangeCheck())
            {
                _cache.Clear();
                GUI.changed = true;
            }
        }

        protected abstract void OnGUIValidatable(Rect position, SerializedProperty property, GUIContent label);

        protected bool IsValid(object prop, out Color color, out Exception exception)
        {
            if (prop is IValidatable validatable)
            {
                if (!_cache.TryGetValue(validatable, out (Validity, Exception) validity))
                {
                    try
                    {
                        validity = (Validity.Invalid, null);
                        validatable.ValidateSelf();
                        validity = (Validity.SelfOnly, null);
                        validatable.ValidateAll();
                        validity = (Validity.Valid, null);
                    }
                    catch (Exception e)
                    {
                        if (e is ValidationException va)
                            validity.Item2 = va.InnerException;
                        else
                            validity.Item2 = e;
                    }

                    _cache.Add(validatable, validity);
                }

                exception = validity.Item2;
                switch (validity.Item1)
                {
                    case Validity.Valid:
                        color = default;
                        return true;
                    case Validity.SelfOnly:
                        color = Color.Lerp(GUI.backgroundColor, Color.red, 0.05f);
                        return false;
                    case Validity.Invalid:
                        color = Color.Lerp(GUI.backgroundColor, Color.red, 1f);
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (prop == null)
            {
                color = default;
                exception = null;
                return true;
            }

            throw new ArgumentException(prop.GetType().ToString());
        }

        enum Validity
        {
            Valid,
            SelfOnly,
            Invalid
        }
    }
}