using System;
using System.Collections;
using System.Collections.Generic;
using Screenplay.Variables;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Screenplay.Commands
{
    [Serializable] public class SetVariable : ICommand
    {
        static Dictionary<IVariable, IVariable> _originalValues = new();

        public UInterface<IVariable> From;

        [SerializeReference, SerializeReferenceButton]
        public IValue To;

        public IEnumerable<(string, IValidatable)> GetSubValues()
        {
            yield return (nameof(From), From.Reference);
            yield return (nameof(To), To);
        }

        public void ValidateSelf()
        {
            if (From.Reference == null)
                throw new NullReferenceException("From");
            if (To == null)
                throw new NullReferenceException("To");

            if (From.Reference != null && !From.Reference.CanBeSetTo(To))
                throw new InvalidOperationException($"Cannot set {From.Reference} to {To}");
        }

        public IEnumerable Run(Stage stage)
        {
            #if UNITY_EDITOR
            if (From.Reference is ScriptableObject scriptableObject && _originalValues.ContainsKey(From.Reference) == false)
                _originalValues.Add(From.Reference, (IVariable)Object.Instantiate(scriptableObject));
            #endif

            From.Reference.SetTo(To);
            yield break;
        }

        public string GetInspectorString() => $"Set {(From.Reference?.GetInspectorString() ?? "??")} to {To?.GetInspectorString()}";

#if UNITY_EDITOR
        static SetVariable()
        {
            UnityEditor.EditorApplication.playModeStateChanged += change =>
            {
                if(change != UnityEditor.PlayModeStateChange.ExitingPlayMode)
                    return;

                // Scriptable object persist changes across session, we don't want that for variables so we're manually resetting them
                foreach (var (recipient, originalValue) in _originalValues)
                    recipient.SetTo(originalValue);
            };
        }
#endif
    }
}