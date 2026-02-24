using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Screenplay.Editor
{
    public class SceneReferenceDrawer : OdinValueDrawer<SceneReference>
    {
        private static readonly GUIContent s_open = new("Open", "Open this scene");
        private static readonly GUIContent s_set = new("Set", "Set to the active scene");

        protected override void DrawPropertyLayout(GUIContent? label)
        {
            GUILayout.BeginHorizontal();

            var value = ValueEntry.SmartValue;
            var obj = value.IsValid() ? AssetDatabase.LoadAssetAtPath<SceneAsset>(value.Path) : null;
            var newObj = EditorGUILayout.ObjectField(label ?? GUIContent.none, obj, typeof(SceneAsset), allowSceneObjects: false);

            if (obj != newObj)
            {
                ValueEntry.SmartValue = new SceneReference((SceneAsset)newObj);
            }

            GUILayout.BeginHorizontal(GUILayoutOptions.MaxWidth(100));

            if (value.IsValid()
                && EditorSceneManager.GetSceneByPath(value.Path).IsValid() == false
                && GUILayout.Button(s_open))
            {
                EditorSceneManager.OpenScene(value.Path);
            }

            if ((value.IsValid() == false || EditorSceneManager.GetSceneByPath(value.Path).IsValid() == false)
                && GUILayout.Button(s_set))
            {
                var s = EditorSceneManager.GetActiveScene();

                if (string.IsNullOrEmpty(s.path))
                {
                    Debug.LogError("Scene is not an asset");
                    return;
                }

                ValueEntry.SmartValue = new SceneReference(AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path));
            }

            GUILayout.EndHorizontal();

            GUILayout.EndHorizontal();
        }
    }
}
