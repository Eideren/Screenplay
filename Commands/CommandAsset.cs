using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Screenplay.Commands
{
    [Serializable, CreateAssetMenu(menuName = "Screenplay/CommandAsset")]
    public class CommandAsset : ScriptableObject, ICommand
    {
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
    }
}