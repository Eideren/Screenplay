using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Screenplay.Commands
{
    [Serializable] public class NonBlocking : ICommand
    {
        [Tooltip("Run this command and continue the scenario right away instead of waiting for the command to finish"), SerializeReference, SerializeReferenceButton]
        public ICommand Command;

        public void ValidateSelf()
        {
            if (Command == null)
                throw new NullReferenceException("Command");
        }

        public IEnumerable<(string name, IValidatable validatable)> GetSubValues()
        {
            yield return (nameof(Command), Command);
        }

        public string GetInspectorString() => $"Non blocking {Command?.GetInspectorString()}";

        public IEnumerable Run(Stage stage)
        {
            stage.StartCoroutine(Command.Run(stage).GetEnumerator());
            yield break;
        }
    }
}