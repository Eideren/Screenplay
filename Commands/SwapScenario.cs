using System;
using System.Collections;
using System.Collections.Generic;

namespace Screenplay.Commands
{
    [Serializable] public class SwapScenario : ICommand
    {
        public Scenario Scenario;

        public void ValidateSelf()
        {
            if (Scenario == null)
                throw new NullReferenceException("Scenario");
        }

        public IEnumerable<(string name, IValidatable validatable)> GetSubValues()
        {
            yield return (nameof(Scenario), Scenario);
        }

        public IEnumerable Run(Stage stage)
        {
            Stage.NewStageFrom(stage, Scenario);
            stage.CloseAndFree();
            yield break;
        }

        public string GetInspectorString() => $"Swap to {(Scenario == null ? "null" : Scenario.name)}";
    }
}