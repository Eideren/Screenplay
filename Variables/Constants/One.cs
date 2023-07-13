using System;
using System.Collections.Generic;

namespace Screenplay.Variables.Constants
{
    [Serializable] public sealed class One : IInt
    {
        public int Value => 1;
        public void ValidateSelf() { }
        public IEnumerable<(string, IValidatable)> GetSubValues() => this.NoSubValues();
        public string GetInspectorString() => "1";
    }
}