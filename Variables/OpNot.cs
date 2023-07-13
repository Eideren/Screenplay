using System;
using System.Collections.Generic;
using UnityEngine;

namespace Screenplay.Variables
{
    [Serializable] public sealed class OpNot : BinaryOperator
    {
        [SerializeReference, SerializeReferenceButton]
        public IBool A;

        public override bool Value => !A.Value;
        public override void ValidateSelf() { }

        public override IEnumerable<(string, IValidatable)> GetSubValues()
        {
            yield return (nameof(A), A);
        }

        public override string GetInspectorString() => $"not {A?.GetInspectorString()}";
    }
}