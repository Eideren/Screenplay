using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceUI
{
    public static class SerializeReferenceGenericSelectionMenu
    {
        public static void ShowContextMenuForManagedReference(this SerializedProperty property, Rect position, Action onSelection, bool nicifyNames, IEnumerable<Func<Type, bool>> filters = null)
        {
            NewContextMenu(filters, property, onSelection, nicifyNames).DropDown(position);
        }

        public static void ShowContextMenuForManagedReference(this SerializedProperty property, Action onSelection, bool nicifyNames, IEnumerable<Func<Type, bool>> filters = null)
        {
            NewContextMenu(filters, property, onSelection, nicifyNames).ShowAsContext();
        }

        static GenericMenu NewContextMenu(IEnumerable<Func<Type, bool>> enumerableFilters, SerializedProperty property, Action onSelection, bool nicifyNames)
        {
            GenericMenu contextMenu = new();
            List<Func<Type, bool>> filters = enumerableFilters.ToList();
            contextMenu.AddItem(new GUIContent("Null"), false, delegate
            {
                property.serializedObject.Update();
                property.managedReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
                onSelection?.Invoke();
            });
            Type fieldType = property.GetManagedReferenceFieldType();
            IEnumerable<Type> appropriateTypes = ManagedReferenceUtility.GetAppropriateTypesForAssigningToManagedReference(fieldType, filters);
            foreach (Type appropriateType in appropriateTypes)
            {
                string tAsString = appropriateType.ToString();
                string entryName = tAsString[(tAsString.LastIndexOf('.') + 1)..];
                if (nicifyNames)
                    entryName = ObjectNames.NicifyVariableName(entryName);
                contextMenu.AddItem(new GUIContent(entryName), false, delegate
                {
                    property.AssignNewInstanceOfTypeToManagedReference(appropriateType);
                    property.isExpanded = true;
                    onSelection?.Invoke();
                });
            }

            return contextMenu;
        }
    }
}