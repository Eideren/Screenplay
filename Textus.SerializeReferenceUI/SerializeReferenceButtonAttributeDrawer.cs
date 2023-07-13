using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceUI
{
    [CustomPropertyDrawer(typeof(SerializeReferenceButtonAttribute), false)]
    public class SerializeReferenceButtonAttributeDrawer : PropertyDrawer
    {
        static Func<Type, Type> typeToDrawerType;
        (PropertyDrawer drawer, Type type) cachedData;
        int version;

        bool GetDrawerForObject(object obj, out PropertyDrawer drawer)
        {
            try
            {
                drawer = null;
                if (obj == null)
                    return false;
                if (obj.GetType() == cachedData.type)
                {
                    drawer = cachedData.drawer;
                    return drawer != null;
                }

                cachedData.type = obj.GetType();
                if (typeToDrawerType == null)
                {
                    Type type = typeof(Editor).Assembly.GetType("UnityEditor.ScriptAttributeUtility");
                    MethodInfo mi = type.GetMethod("GetDrawerTypeForType", BindingFlags.Static | BindingFlags.NonPublic);
                    typeToDrawerType = (Func<Type, Type>)mi.CreateDelegate(typeof(Func<Type, Type>));
                }

                Type drawerType = typeToDrawerType(obj.GetType());
                if ((object)drawerType == null)
                    return false;
                cachedData.drawer = drawer = (PropertyDrawer)Activator.CreateInstance(drawerType);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                drawer = null;
                return false;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (GetDrawerForObject(property.managedReferenceValue, out PropertyDrawer drawer))
            {
                try
                {
                    return EditorGUIUtility.singleLineHeight + drawer.GetPropertyHeight(property, label);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw;
                }
            }

            return EditorGUI.GetPropertyHeight(property, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect labelPosition = new(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            if (label != GUIContent.none)
                EditorGUI.LabelField(labelPosition, label);
            else
                labelPosition = default;
            IEnumerable<Func<Type, bool>> typeRestrictions = SerializedReferenceUIDefaultTypeRestrictions.GetAllBuiltInTypeRestrictions(fieldInfo);
            Rect buttonPosition = new(position.x + labelPosition.width, position.y, position.width - labelPosition.width, EditorGUIUtility.singleLineHeight);
            DrawSelectionButtonForManagedReference(property, buttonPosition, typeRestrictions);
            if (GetDrawerForObject(property.managedReferenceValue, out PropertyDrawer drawer))
            {
                position.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.indentLevel++;
                try
                {
                    position.height = drawer.GetPropertyHeight(property, label);
                    drawer.OnGUI(position, property, GUIContent.none);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw;
                }

                EditorGUI.indentLevel--;
            }
            else
                EditorGUI.PropertyField(position, property, GUIContent.none, true);

            if (version != 0)
            {
                GUI.changed = true;
                version = 0;
            }

            EditorGUI.EndProperty();
        }

        void DrawSelectionButtonForManagedReference(SerializedProperty property, Rect position, IEnumerable<Func<Type, bool>> filters = null)
        {
            SerializeReferenceButtonAttribute attributeData = (SerializeReferenceButtonAttribute)attribute;
            Color backgroundColor = new(0.1f, 0.55f, 0.9f, 1f);;
            Color storedColor = GUI.backgroundColor;
            GUI.backgroundColor = backgroundColor;
            (string, string) names = ManagedReferenceUtility.GetSplitNamesFromTypename(property.managedReferenceFullTypename);
            string className = string.IsNullOrEmpty(names.Item2) ? "Null (Assign)" : names.Item2;
            (string assemblyName, _) = names;
            string buttonString;
            if (property.propertyType == SerializedPropertyType.ManagedReference && property.managedReferenceValue is IInspectorString iString)
            {
                try
                {
                    buttonString = iString.GetInspectorString();
                }
                catch (Exception e)
                {
                    buttonString = className[(className.LastIndexOf('.') + 1)..];
                    Debug.LogException(e);
                }
            }
            else
                buttonString = className[(className.LastIndexOf('.') + 1)..];

            if (GUI.Button(position, new GUIContent(buttonString, $"{className}  ({assemblyName})")))
                property.ShowContextMenuForManagedReference(position, delegate { version++; }, attributeData.nicifyNames, filters);
            GUI.backgroundColor = storedColor;
        }
    }
}