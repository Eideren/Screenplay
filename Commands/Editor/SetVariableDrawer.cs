using System;
using UnityEditor;
using UnityEngine;

namespace Screenplay.Commands
{
    [CustomPropertyDrawer(typeof(SetVariable), false)]
    public class SetVariableDrawer : ValidatableDrawer
    {
        readonly UInterfaceCreatorField _uInterfaceDrawer = new();

        protected override void OnGUIValidatable(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty fromProp = property.FindPropertyRelative("From");
            SerializedProperty toProp = property.FindPropertyRelative("To");
            Rect left = position;
            left.height = EditorGUI.GetPropertyHeight(fromProp);
            left.width = EditorGUIUtility.labelWidth;
            left.SplitWithRightOf(20f, out Rect fromField, out Rect toText);
            using (new GUIBackgroundColorScope(GUI.backgroundColor))
            {
                if (!IsValid(property.boxedValue, out Color color, out Exception e))
                    GUI.backgroundColor = color;
                if (e != null)
                    EditorGUI.LabelField(position, new GUIContent("", $"Issue: {e.Message} ({e.GetType().Name})"));
                _uInterfaceDrawer.Draw(fromField, fromProp, "Screenplay/Variables");
                EditorGUI.HandlePrefixLabel(toText, toText, new GUIContent(" to"));
                EditorGUI.PropertyField(position, toProp, new GUIContent());
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty commandProp = property.FindPropertyRelative("To");
            return EditorGUI.GetPropertyHeight(commandProp);
        }
    }
}