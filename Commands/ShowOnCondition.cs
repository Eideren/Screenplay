using System;
using System.Collections;
using System.Collections.Generic;
using Screech;
using Screenplay.Variables;
using UnityEngine;

namespace Screenplay.Commands
{
    [Serializable] public class ShowOnCondition : ICommand
    {
        [SerializeReference, SerializeReferenceButton]
        public IBool Condition;

        public IEnumerable<(string name, IValidatable validatable)> GetSubValues()
        {
            yield return (nameof(Condition), Condition);
        }

        public void ValidateSelf()
        {
            if (Condition == null)
                throw new InvalidOperationException("Condition is null");
        }

        public IEnumerable Run(Stage stage) => throw new InvalidOperationException($"This should have been converted into a {typeof(ShowWhen)}");

        public string GetInspectorString() => $"Show when {Condition?.GetInspectorString()}";

        public bool Evaluate() => Condition.Value;
    }
}