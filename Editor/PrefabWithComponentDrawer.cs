using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Screenplay
{
    public class PrefabWithComponentDrawer<T> : OdinAttributeDrawer<PrefabWithComponentAttribute, T>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var value = ValueEntry.SmartValue;
            var rect = GUILayoutUtility.GetRect(label, EditorStyles.objectField);
            rect = EditorGUI.PrefixLabel(rect, label);

            GUI.Box(rect, GUIContent.none, EditorStyles.objectField);

            Event evt = Event.current;
            if (rect.Contains(evt.mousePosition) && evt.type is EventType.DragUpdated or EventType.DragPerform)
            {
                Object? dragged = DragAndDrop.objectReferences.FirstOrDefault(o => typeof(T).IsAssignableFrom(o.GetType()));

                if (dragged != null)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        ValueEntry.SmartValue = (T)(object)dragged;
                        evt.Use();
                    }
                }
            }

            GUIContent? content = value != null ? EditorGUIUtility.ObjectContent((Object)(object)value, typeof(T)) : new GUIContent("none");

            EditorGUI.LabelField(rect, content);

            var pickerRect = new Rect(rect.xMax - 18, rect.y, 18, rect.height);
            if (GUI.Button(pickerRect, GUIContent.none, ObjectPickerButton))
                CustomizedPicker.Show(GetItems(Property.Attributes.Any(x => x.GetType().Name.Contains("Nullable"))), rect, picked => ValueEntry.SmartValue = picked!);

            var pingRect = rect;
            pingRect.width -= pickerRect.width;
            if (pingRect.Contains(evt.mousePosition) && evt.type is EventType.MouseDown && value != null)
                EditorGUIUtility.PingObject((Object)(object)value);
        }

        private static IEnumerable<T?> GetItems(bool allowNull)
        {
            if (allowNull)
                yield return default;

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null && prefab.GetComponent<T>() is {} c && c != null)
                {
                    yield return c;
                }
            }
        }

        static GUIStyle? objectPickerButton;

        static GUIStyle ObjectPickerButton
        {
            get
            {
                if (objectPickerButton == null)
                {
                    var p = typeof(EditorStyles).GetProperty("objectFieldButton", BindingFlags.Static | BindingFlags.NonPublic);
                    objectPickerButton = (GUIStyle)p!.GetValue(null);
                }

                return objectPickerButton;
            }
        }
    }
}
