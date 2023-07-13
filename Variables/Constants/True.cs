using System;
using System.Collections.Generic;

namespace Screenplay.Variables.Constants
{
    [Serializable] public sealed class True : IBool
    {
        public bool Value => true;
        public void ValidateSelf() { }
        public IEnumerable<(string, IValidatable)> GetSubValues() => this.NoSubValues();
        public string GetInspectorString() => "True";
    }
}