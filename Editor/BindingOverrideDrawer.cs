using System;
using Screenplay.Commands;
using UnityEditor;
using UnityEngine;

namespace Screenplay.Editor
{
    [CustomPropertyDrawer(typeof(BindingOverride), false)]
    public class BindingOverrideDrawer : ValidatableDrawer
    {
        protected override void OnGUIValidatable(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty nameProp = property.FindPropertyRelative("Name");
            SerializedProperty commandProp = property.FindPropertyRelative("Command");
            Rect left = position;
            left.height = EditorGUI.GetPropertyHeight(nameProp);
            left.width = EditorGUIUtility.labelWidth;
            left.x += 6f;
            left.width -= 6f;
            using (new GUIBackgroundColorScope(GUI.backgroundColor))
            {
                if (!IsValid(commandProp.boxedValue, out Color color, out Exception e))
                    GUI.backgroundColor = color;
                if (e != null)
                    EditorGUI.LabelField(position, new GUIContent("", $"Issue: {e.Message} ({e.GetType().Name})"));
                EditorGUI.PropertyField(left, nameProp, GUIContent.none);
                EditorGUI.PropertyField(position, commandProp, new GUIContent());
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty commandProp = property.FindPropertyRelative("Command");
            return EditorGUI.GetPropertyHeight(commandProp);
        }
    }
}