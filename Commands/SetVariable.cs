using System;
using System.Collections;
using System.Collections.Generic;
using Screenplay.Variables;
using UnityEngine;

namespace Screenplay.Commands
{
    [Serializable] public class SetVariable : ICommand
    {
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
            From.Reference.SetTo(To);
            yield break;
        }

        public string GetInspectorString() => $"Set {(From.Reference?.GetInspectorString() ?? "??")} to {To?.GetInspectorString()}";

    }
}