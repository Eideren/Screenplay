using System;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Screenplay.Editor
{
    [ScriptedImporter(1, "scenario")] public class ScenarioImporter : ScriptedImporter
    {
        public Scenario Scenario;
        public Scenario.Binding[] Bindings = Array.Empty<Scenario.Binding>();

        public override void OnImportAsset(AssetImportContext ctx)
        {
            if (Scenario == null)
                Scenario = ScriptableObject.CreateInstance<Scenario>();
            Scenario.Bindings = Bindings;
            Scenario.Content = File.ReadAllText(ctx.assetPath);
            ctx.AddObjectToAsset("Scenario", Scenario);
            ctx.SetMainObject(Scenario);
        }
    }
}