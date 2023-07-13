using System;
using System.Collections.Generic;

namespace Screenplay.Variables.Constants
{
    [Serializable] public sealed class Zero : IInt
    {
        public int Value => 0;
        public void ValidateSelf() { }
        public IEnumerable<(string, IValidatable)> GetSubValues() => this.NoSubValues();
        public string GetInspectorString() => "0";
    }
}