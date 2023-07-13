using System;
using Screenplay.Commands;
using UnityEditor;
using UnityEngine;

namespace Screenplay.Variables
{
    [CustomPropertyDrawer(typeof(VariableRef), false)]
    public class VariableRefDrawer : ValidatableDrawer
    {
        readonly UInterfaceCreatorField uInterfaceDrawer = new();

        protected override void OnGUIValidatable(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty variableField = property.FindPropertyRelative("Variable");
            position.SplitWithLeftOf(EditorGUIUtility.labelWidth, out Rect left, out Rect right);
            using (new GUIBackgroundColorScope(GUI.backgroundColor))
            {
                if (!IsValid(property.boxedValue, out Color color, out Exception e))
                    GUI.backgroundColor = color;
                if (e != null)
                    EditorGUI.LabelField(position, new GUIContent("", $"Issue: {e.Message} ({e.GetType().Name})"));
                uInterfaceDrawer.Draw(right, variableField, "Screenplay/Variables");
                EditorGUI.LabelField(left, variableField.name);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty commandProp = property.FindPropertyRelative("Variable");
            return EditorGUI.GetPropertyHeight(commandProp);
        }
    }
}