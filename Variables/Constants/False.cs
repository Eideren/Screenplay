using System;
using System.Collections.Generic;

namespace Screenplay.Variables.Constants
{
    [Serializable] public sealed class False : IBool
    {
        public bool Value => false;
        public void ValidateSelf() { }
        public IEnumerable<(string, IValidatable)> GetSubValues() => this.NoSubValues();
        public string GetInspectorString() => "False";
    }
}