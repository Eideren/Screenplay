using System;
using System.Collections.Generic;
using UnityEngine;

namespace Screenplay.Variables
{
    [Serializable] public sealed class OpAnd : BinaryOperator
    {
        [SerializeReference, SerializeReferenceButton]
        public IBool A;

        [SerializeReference, SerializeReferenceButton]
        public IBool B;

        public override bool Value => A.Value && B.Value;
        public override void ValidateSelf() { }

        public override IEnumerable<(string, IValidatable)> GetSubValues()
        {
            yield return (nameof(A), A);
            yield return (nameof(B), B);
        }

        public override string GetInspectorString() => $"{A?.GetInspectorString()} and {B?.GetInspectorString()}";
    }
}