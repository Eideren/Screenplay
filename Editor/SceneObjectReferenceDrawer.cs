using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Screenplay.Editor
{
    public class SceneObjectReferenceDrawer<T> : OdinValueDrawer<SceneObjectReference<T>> where T : Object
    {
        private static readonly GUIContent s_unloadedContent = new("Open scene ?", "This reference is not loaded, press this button to swap to its scene");
        private static readonly GUIContent s_clearContent = new("Clear ?", "This reference is not loaded, press this button to unassign it");

        protected override void DrawPropertyLayout(GUIContent? label)
        {
            var value = ValueEntry.SmartValue;
            if (Property.Info.GetAttribute<RequiredAttribute>() is not null && value.Empty())
                SirenixEditorGUI.ErrorMessageBox($"{Property.NiceName} is required");

            Rect rect = EditorGUILayout.GetControlRect();
            value.TryGet(out T? obj, out var state);
            if (state == ReferenceState.SceneUnloaded)
            {
                if (label != null)
                    rect = EditorGUI.PrefixLabel(rect, label);

                if (GUI.Button(rect.AlignLeft(rect.width / 3*2), s_unloadedContent))
                    EditorSceneManager.OpenScene(value.ScenePath);
                if (GUI.Button(rect.AlignRight(rect.width / 3), s_clearContent))
                    ValueEntry.SmartValue = new();
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                if (label == null)
                    obj = (T)EditorGUI.ObjectField(rect, obj, typeof(T), true);
                else
                    obj = (T)EditorGUI.ObjectField(rect, label, obj, typeof(T), true);
                if (EditorGUI.EndChangeCheck())
                {
                    if (obj is null || obj.Equals(null))
                        this.ValueEntry.SmartValue = new();
                    else if (typeof(UnityEngine.Component).IsAssignableFrom(typeof(T)))
                        this.ValueEntry.SmartValue = new((UnityEngine.Component)(Object)obj);
                    else
                        this.ValueEntry.SmartValue = new((GameObject)(Object)obj);
                }
            }
        }
    }
}
