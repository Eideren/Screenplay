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

        [Tooltip("Update the textbox if the text changes")]
        public bool Continuous;

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
            if (Continuous)
            {
                Cache cache = new() { insertion = stage.CharacterIndex, previous = text };
                stage.OnTickForLine += () => OnTickForLine(stage, cache);
            }

            yield break;
        }

        void OnTickForLine(Stage stage, Cache cache)
        {
            string text = Text.EvalString();
            if (string.Equals(cache.previous, text, StringComparison.Ordinal) == false)
            {
                var str = stage.ActiveFeed.text.Remove(cache.insertion, cache.previous.Length);
                stage.ActiveFeed.text = str.Insert(cache.insertion, text);
            }
        }

        class Cache
        {
            public int insertion;
            public string previous;
        }
    }
}