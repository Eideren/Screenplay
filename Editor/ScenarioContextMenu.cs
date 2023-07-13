using System.IO;
using UnityEditor;
using UnityEngine;

namespace Screenplay.Editor
{
    public static class ScenarioContextMenu
    {
        [MenuItem("Assets/Create/Screenplay/Scenario")]
        static void CreateAsset(MenuCommand command)
        {
            if (Selection.assetGUIDs.Length == 1)
            {
                string dir = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
                if (AssetDatabase.IsValidFolder(dir))
                {
                    string path = Path.Combine(dir, "MyScenario.scenario");
                    path = AssetDatabase.GenerateUniqueAssetPath(path);
                    Scenario asset = ScriptableObject.CreateInstance<Scenario>();
                    File.WriteAllText(path, asset.Content);
                    AssetDatabase.ImportAsset(path);
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<Scenario>(path);
                }
            }
        }
    }
}