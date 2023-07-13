using System;
using System.Collections;
using System.Collections.Generic;
using Screenplay.Variables;
using UnityEngine;

namespace Screenplay.Commands
{
    [Serializable] public class InsertText : ICommand
    {
        [SerializeReference, SerializeReferenceButton]
        public IValue Text;

        public void ValidateSelf()
        {
            if (Text == null)
                throw new NullReferenceException("Text");
        }

        public IEnumerable<(string name, IValidatable validatable)> GetSubValues()
        {
            yield return (nameof(Text), Text);
        }

        public string GetInspectorString() => $"Insert {(Text != null ? Text.GetInspectorString() : "null")}";

        public IEnumerable Run(Stage stage)
        {
            string text = Text.EvalString();
            stage.ActiveFeed.text = stage.ActiveFeed.text.Insert(stage.CharacterIndex, text);
            yield break;
        }
    }
}