using System;
using System.Collections.Generic;
using Screenplay.Variables;
using UnityEngine;

namespace Screenplay.Commands
{
    [Serializable] public class ShowOnCondition : IShowOnCondition
    {
        [SerializeReference, SerializeReferenceButton]
        public IBool Condition;

        public bool Show(Stage stage, Screech.Node line) => Condition.Value;
        public IEnumerable<(string name, IValidatable validatable)> GetSubValues()
        {
            yield return (nameof(Condition), Condition);
        }

        public void ValidateSelf()
        {
            if (Condition == null)
                throw new InvalidOperationException("Condition is null");
        }

        public string GetInspectorString() => $"Show when {Condition?.GetInspectorString()}";
    }
}