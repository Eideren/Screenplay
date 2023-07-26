using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Screenplay
{
    /// <summary>
    /// A <see cref="Screenplay.Scenario"/> inside a Scene, doesn't do anything by itself,
    /// just a nice editor to manage <see cref="ScenarioInScene.Overrides"/>
    /// </summary>
    public class ScenarioInScene : MonoBehaviour, ISerializationCallbackReceiver, IValidatable
    {
        public Scenario Scenario;
        /// <summary>
        /// Overrides commands with matching names inside of the <see cref="Scenario"/> with another command
        /// </summary>
        public BindingOverride[] Overrides = Array.Empty<BindingOverride>();

        public void Start()
        {
            new Stage(this, Scenario);
        }

        void OnValidate()
        {
            if (Scenario == null)
                return;
            Scenario.Binding[] bindings = Scenario.Bindings;
            foreach (Scenario.Binding binding in bindings)
            {
                if (binding.IsSceneBound && !Overrides.Any(x => x.Name == binding.Name))
                {
                    Array.Resize(ref Overrides, Overrides.Length + 1);
                    Overrides[^1] = new BindingOverride
                    {
                        Name = binding.Name
                    };
                }
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += ValidationPostDeserialize;

            void ValidationPostDeserialize()
            {
                UnityEditor.EditorApplication.update -= ValidationPostDeserialize;
                try
                {
                    this.ValidateAll();
                }
                catch (ValidationException e)
                {
                    Debug.LogException(e, e.ClosestObj ?? this);
                }
            }
#endif
        }

        public void ValidateSelf()
        {
            BindingOverride[] overrides = Overrides;
            for (int i = 0; i < overrides.Length; i++)
            {
                BindingOverride binding = overrides[i];
                if (binding.Command == null)
                    throw new InvalidOperationException($"Command '{binding.Name}' is null");
            }

            if (Scenario == null)
                return;
            Scenario.Binding[] bindings = Scenario.Bindings;
            for (int j = 0; j < bindings.Length; j++)
            {
                Scenario.Binding binding2 = bindings[j];
                if (binding2.IsSceneBound && !Overrides.Any(x => x.Name == binding2.Name))
                    throw new InvalidOperationException($"'{Scenario}' has scene command '{binding2.Name}' but {this} does not handle it");
            }
        }

        public IEnumerable<(string, IValidatable)> GetSubValues()
        {
            yield return ("File", Scenario);
            BindingOverride[] overrides = Overrides;
            for (int i = 0; i < overrides.Length; i++)
            {
                BindingOverride binding = overrides[i];
                yield return (binding.Name, binding.Command);
            }
        }
    }
}