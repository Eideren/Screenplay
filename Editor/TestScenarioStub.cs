using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Screenplay.Editor
{
    public class TestScenarioStub : EditorWindow
    {
        public Scenario Scenario;
        void OnGUI()
        {
            if (!EditorApplication.isPlaying)
                return;
            Close();

            var scenarioInScene = new GameObject(nameof(TestScenarioStub)).AddComponent<ScenarioTesterComp>();
            scenarioInScene.Scenario = Scenario;
            var overrides = new List<BindingOverride>();
            foreach (var binding in Scenario.Bindings)
            {
                if (binding.IsSceneBound)
                {
                    overrides.Add(new()
                    {
                        Name = binding.Name,
                        Command = new DoNothing()
                    });
                }
            }

            if (overrides.Count > 0)
                Debug.LogWarning($"Commands {string.Join(", ", overrides.Select(x => x.Name))} were not set, we'll ignore them");

            scenarioInScene.Overrides = overrides.ToArray();
            var stage = new Stage(scenarioInScene);
            scenarioInScene.StartCoroutine(Cleanup());

            IEnumerator Cleanup()
            {
                while (stage.IsClosed == false)
                    yield return null;
                Destroy(scenarioInScene);
            }
        }

        public class ScenarioTesterComp : ScenarioInScene { }

        class DoNothing : ICommand
        {
            public void ValidateSelf(){}

            public IEnumerable<(string name, IValidatable validatable)> GetSubValues() => this.NoSubValues();

            public string GetInspectorString() => "Do Nothing";

            public IEnumerable Run(Stage stage)
            {
                yield break;
            }
        }
    }
}