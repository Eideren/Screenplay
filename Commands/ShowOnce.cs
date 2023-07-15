using System;
using System.Collections.Generic;

namespace Screenplay.Commands
{
    [Serializable] public class ShowOnce : IShowOnCondition
    {
        readonly HashSet<Screech.Node> _visited = new();

        public bool Show(Stage stage, Screech.Node line)
        {
            // This object instance might be used across multiple lines,
            // use a collection to store whether it visited each lines
            return _visited.Add(line);
        }

        public void ValidateSelf(){ }

        public IEnumerable<(string name, IValidatable validatable)> GetSubValues() => this.NoSubValues();

        public string GetInspectorString() => "Show this line only once";
    }
}