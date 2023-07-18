using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Screenplay.Editor
{
    public class TestScenarioStub : EditorWindow
    {
        public Scenario ScenarioRef;
        void OnGUI()
        {
            if (!EditorApplication.isPlaying)
                return;
            Close();

            if (FindObjectOfType<ScenarioTesterComp>() is ScenarioTesterComp instance)
            {
                if(Stage.IsRunning(instance.Scenario, out var otherStage))
                    otherStage.CloseAndFree();
                Destroy(instance.gameObject);
            }

            Scenario newScenario = CreateInstance<Scenario>();
            newScenario.Content = ScenarioRef.Content;
            newScenario.Bindings = ScenarioRef.Bindings.ToArray();
            var overrides = new List<BindingOverride>();
            foreach (var binding in newScenario.Bindings)
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

            var missingCommands = new List<string>();
            foreach (Match match in Scenario.CommandPattern.Matches(ScenarioRef.Content))
            {
                var commandName = match.Groups[1].Value;
                if (ScenarioRef.Bindings.FirstOrDefault(x => x.Name == commandName).Name != commandName)
                {
                    var binding = new Scenario.Binding() { Name = commandName, Command = new DoNothing() };
                    ScenarioRef.Bindings = ScenarioRef.Bindings.Append(binding).ToArray();
                    missingCommands.Add(commandName);
                }
            }

            if (overrides.Count > 0)
                Debug.LogWarning($"Scene bound commands '{string.Join(", ", overrides.Select(x => x.Name))}' are skipped when testing");

            if (missingCommands.Count > 0)
                Debug.LogWarning($"Commands '{string.Join(", ", missingCommands)}' are not bound, they will be ignored");

            var scenarioInScene = new GameObject(nameof(TestScenarioStub)).AddComponent<ScenarioTesterComp>();
            scenarioInScene.Scenario = ScenarioRef;
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