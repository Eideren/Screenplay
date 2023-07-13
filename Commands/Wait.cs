using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Screenplay.Commands
{
    [Serializable] public class Wait : ICommand
    {
        public float Duration;
        public void ValidateSelf() { }
        public IEnumerable<(string name, IValidatable validatable)> GetSubValues() => this.NoSubValues();
        public string GetInspectorString() => $"Wait for {Duration}";

        public IEnumerable Run(Stage stage)
        {
            float counter = Duration;
            float num;
            do
            {
                yield return null;
                counter = num = counter - Time.deltaTime;
            } while (num > 0f);
        }
    }
}