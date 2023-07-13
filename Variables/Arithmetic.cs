using System;
using System.Collections.Generic;
using UnityEngine;

namespace Screenplay.Variables
{
    [Serializable] public class Arithmetic : IValue<double>, INumber
    {
        public enum OperatorType
        {
            Add,
            Sub,
            Multiply,
            Divide
        }

        public OperatorType Operator = OperatorType.Add;

        [SerializeReference, SerializeReferenceButton]
        public INumber A;

        [SerializeReference, SerializeReferenceButton]
        public INumber B;

        public double GetNumber() => Value;

        public double Value
        {
            get
            {
                double result = Operator switch
                {
                    OperatorType.Add => A.GetNumber() + B.GetNumber(),
                    OperatorType.Sub => A.GetNumber() - B.GetNumber(),
                    OperatorType.Multiply => A.GetNumber() * B.GetNumber(),
                    OperatorType.Divide => A.GetNumber() / B.GetNumber(),
                    _ => throw new ArgumentOutOfRangeException()
                };

                return result;
            }
        }

        public string GetInspectorString()
        {
            string result = Operator switch
            {
                OperatorType.Add => $"{A?.GetInspectorString()} + {B?.GetInspectorString()}",
                OperatorType.Sub => $"{A?.GetInspectorString()} - {B?.GetInspectorString()}",
                OperatorType.Multiply => $"{A?.GetInspectorString()} * {B?.GetInspectorString()}",
                OperatorType.Divide => $"{A?.GetInspectorString()} / {B?.GetInspectorString()}",
                _ => throw new ArgumentOutOfRangeException()
            };

            return result;
        }

        public void ValidateSelf()
        {
            if (A == null)
                throw new InvalidOperationException($"{nameof(A)} is null");
            if (B == null)
                throw new InvalidOperationException($"{nameof(B)} is null");
        }

        public IEnumerable<(string, IValidatable)> GetSubValues()
        {
            yield return (nameof(A), A);
            yield return (nameof(B), B);
        }
    }
}