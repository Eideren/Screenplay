using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Screenplay.Editor
{
    [CustomPropertyDrawer(typeof(UInterface<>), false)]
    public class UInterfaceDrawer : PropertyDrawer
    {
        static readonly Dictionary<Type, (Type type, Type[] comps, Type[] assets)> _filter = new();
        static int version;
        int localVersion;
        Type GetGenericType() => fieldInfo.FieldType.IsArray ? fieldInfo.FieldType.GetElementType().GetGenericArguments()[0] : fieldInfo.FieldType.GetGenericArguments()[0];

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!_filter.TryGetValue(fieldInfo.FieldType, out (Type type, Type[] comps, Type[] assets) data))
            {
                Type type = GetGenericType();
                Type[] allTypes = TypesImplementingInterface(type).ToArray();
                Type[] components = allTypes.Where(x => typeof(Component).IsAssignableFrom(x)).ToArray();
                Type[] assets = allTypes.Where(x => !typeof(Component).IsAssignableFrom(x)).ToArray();
                _filter.Add(fieldInfo.FieldType, data = (type, components, assets));
            }

            SerializedProperty unityProp = property.FindPropertyRelative("UnityObj");
            Object obj2 = unityProp.objectReferenceValue;
            GUIContent objLabel = obj2 ? new GUIContent(obj2.ToString(), AssetPreview.GetMiniThumbnail(obj2)) : new GUIContent($"None ({data.type.Name})");
            unityProp.objectReferenceValue = Draw(position, label, unityProp.objectReferenceValue, objLabel, data.type, out bool pressedObjectPicker);
            if (pressedObjectPicker)
            {
                Object target = unityProp.serializedObject.targetObject;
                SerializedObject serializedObject = unityProp.serializedObject;
                string propPath = unityProp.propertyPath;
                GameObject go = target as GameObject ?? (target as Component)?.gameObject;
                ObjectPickerDropdown dropdown = new(data.type, data.comps, data.assets, go, new AdvancedDropdownState());
                dropdown.OnOptionPicked += delegate(Object obj)
                {
                    if (data.type.IsInstanceOfType(obj) || obj == null)
                    {
                        serializedObject.FindProperty(propPath).objectReferenceValue = obj;
                        serializedObject.ApplyModifiedProperties();
                        version++;
                    }
                };
                dropdown.Show(position);
            }

            if (localVersion != version)
            {
                GUI.changed = true;
                localVersion = version;
            }
        }

        static IEnumerable<Type> TypesImplementingInterface(Type desiredType)
        {
            return (from asm in AppDomain.CurrentDomain.GetAssemblies() where asm.GetReferencedAssemblies().Any(asmName => AssemblyName.ReferenceMatchesDefinition(asmName, desiredType.Assembly.GetName())) select asm).Concat(new Assembly[1] { desiredType.Assembly }).SelectMany(assembly => assembly.GetTypes()).Where(desiredType.IsAssignableFrom);
        }

        static Object Draw(Rect position, GUIContent label, Object activeObject, GUIContent objectLabel, Type interfaceType, out bool pressedObjectPicker)
        {
            Rect dropBoxRect = EditorGUI.PrefixLabel(position, label);
            Rect buttonRect = dropBoxRect;
            buttonRect.xMin = position.xMax - 20f;
            buttonRect = new RectOffset(-1, -1, -1, -1).Add(buttonRect);
            pressedObjectPicker = false;
            Event ev = Event.current;
            if (GUI.enabled && dropBoxRect.Contains(ev.mousePosition))
            {
                if (ev.type == EventType.MouseDown)
                {
                    bool isMouseOverSelectButton = buttonRect.Contains(ev.mousePosition);
                    Event.current.Use();
                    if (isMouseOverSelectButton)
                        pressedObjectPicker = true;
                    else if (activeObject != null)
                        EditorGUIUtility.PingObject(GetPingableObject(activeObject));
                }
                else if (HandleDragEvents(GetDraggedObjectIfValid(interfaceType), ref activeObject))
                    Event.current.Use();
            }

            GUI.Toggle(dropBoxRect, dropBoxRect.Contains(Event.current.mousePosition) && (bool)GetDraggedObjectIfValid(interfaceType), objectLabel.image ? GUIContent.none : objectLabel, EditorStyles.objectField);
            if ((bool)objectLabel.image)
            {
                Rect iconRect = dropBoxRect;
                iconRect.center += Vector2.right * 3f;
                iconRect.width = 15f;
                Texture icon = objectLabel.image;
                objectLabel.image = null;
                GUIStyle labelStyle = new(EditorStyles.objectField);
                labelStyle.normal.background = Texture2D.blackTexture;
                EditorGUI.LabelField(dropBoxRect, objectLabel, labelStyle);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }

            GUIStyle objectFieldButtonStyle = new("ObjectFieldButton");
            GUI.Button(buttonRect, new GUIContent(""), objectFieldButtonStyle);
            return activeObject;
        }

        static Object GetPingableObject(Object activeObject) => activeObject is Component component ? component.gameObject : activeObject;

        static Object GetDraggedObjectIfValid(Type interfaceType)
        {
            Object[] draggedObjects = DragAndDrop.objectReferences;
            if (draggedObjects.Length != 1)
                return null;
            Object obj = draggedObjects[0];
            return interfaceType.IsInstanceOfType(obj) ? obj : null;
        }

        static bool HandleDragEvents(bool isValidObjectBeingDragged, ref Object activeObject)
        {
            Event ev = Event.current;
            if (ev.type == EventType.DragUpdated)
            {
                if (isValidObjectBeingDragged)
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                else
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                return true;
            }

            if (ev.type == EventType.DragPerform)
            {
                if (isValidObjectBeingDragged)
                {
                    DragAndDrop.AcceptDrag();
                    activeObject = DragAndDrop.objectReferences[0];
                }

                return true;
            }

            if (ev.type == EventType.DragExited)
                return true;
            return false;
        }
    }
}