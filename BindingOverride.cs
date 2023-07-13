using System;
using UnityEngine;

namespace Screenplay
{
    /// <summary> Scene overrides for a <see cref="Scenario.Binding"/>, see <see cref="Scenario.Binding.IsSceneBound"/> </summary>
    [Serializable] public struct BindingOverride
    {
        public string Name;

        [SerializeReference, SerializeReferenceButton]
        public ICommand Command;
    }
}