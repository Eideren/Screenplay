using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Screenplay.Commands
{
    public class CommandComponent : MonoBehaviour, ICommand
    {
        public string Name = "Name of this command";
        [SerializeReference, SerializeReferenceButton]
        public ICommand Command;

        public void ValidateSelf()
        {
            if (Command == null)
                throw new NullReferenceException(nameof(Command));
        }

        public string GetInspectorString() => Command?.GetInspectorString() ?? "null";

        public IEnumerable Run(Stage stage) => Command.Run(stage);

        public IEnumerable<(string name, IValidatable validatable)> GetSubValues()
        {
            yield return (nameof(Command), Command);
        }

        public override string ToString() => string.IsNullOrEmpty(Name) ? base.ToString() : Name;
    }
}