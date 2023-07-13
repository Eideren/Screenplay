using System;
using System.Collections.Generic;
using UnityEngine;

namespace Screenplay.Variables
{
    [Serializable] public class Compare : IBool
    {
        public enum CompType
        {
            Equal,
            NotEqual,
            Greater,
            Smaller,
            GreaterOrEqual,
            SmallerOrEqual
        }

        public CompType Type = CompType.Equal;

        [SerializeReference, SerializeReferenceButton]
        public IValue A;

        [SerializeReference, SerializeReferenceButton]
        public IValue B;

        public bool Value
        {
            get
            {
                if (!A.TryCompare(B, out int val))
                    throw new InvalidOperationException($"Cannot compare {A} to {B}, types do not match");
                CompType type = Type;

                bool result = type switch
                {
                    CompType.Equal => val == 0,
                    CompType.NotEqual => val != 0,
                    CompType.Greater => val > 0,
                    CompType.Smaller => val < 0,
                    CompType.GreaterOrEqual => val >= 0,
                    CompType.SmallerOrEqual => val <= 0,
                    _ => throw new ArgumentOutOfRangeException()
                };

                return result;
            }
        }

        public IEnumerable<(string, IValidatable)> GetSubValues()
        {
            yield return (nameof(A), A);
            yield return (nameof(B), B);
        }

        public void ValidateSelf()
        {
            if (!A.TryCompare(B, out int _))
                throw new InvalidOperationException($"Cannot compare {A} to {B}, types do not match");
        }

        public string GetInspectorString()
        {
            CompType type = Type;

            string compString = type switch
            {
                CompType.Equal => "==",
                CompType.NotEqual => "!=",
                CompType.Greater => ">",
                CompType.Smaller => "<",
                CompType.GreaterOrEqual => ">=",
                CompType.SmallerOrEqual => "<=",
                _ => throw new ArgumentOutOfRangeException()
            };

            return $"{A?.GetInspectorString()} {compString} {B?.GetInspectorString()}";
        }
    }
}