using UnityEditor;
using UnityEngine;

namespace Screenplay.Editor
{
    public class TestScenarioStub : EditorWindow
    {
        public Scenario Scenario;
        void OnGUI()
        {
            if (EditorApplication.isPlaying)
            {
                var scenarioInScene = new GameObject(nameof(TestScenarioStub)).AddComponent<ScenarioTesterComp>();
                scenarioInScene.Scenario = Scenario;
                new Stage(scenarioInScene);
                Close();
            }
        }

        public class ScenarioTesterComp : ScenarioInScene
        {

        }
    }
}