using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SerializeReferenceUI
{
    public static class ManagedReferenceUtility
    {
        public static object AssignNewInstanceOfTypeToManagedReference(this SerializedProperty serializedProperty, Type type)
        {
            object instance = Activator.CreateInstance(type);
            serializedProperty.serializedObject.Update();
            serializedProperty.managedReferenceValue = instance;
            serializedProperty.serializedObject.ApplyModifiedProperties();
            return instance;
        }

        public static IEnumerable<Type> GetAppropriateTypesForAssigningToManagedReference(Type fieldType, List<Func<Type, bool>> filters = null)
        {
            var appropriateTypes = new List<Type>();
            foreach (Type type in TypeCache.GetTypesDerivedFrom(fieldType))
            {
                if (!type.IsSubclassOf(typeof(Object)) && !type.IsAbstract && !type.ContainsGenericParameters && (!type.IsClass || !(type.GetConstructor(Type.EmptyTypes) == null)) && (filters == null || filters.All(f => f?.Invoke(type) ?? true)) && type.IsPublic)
                    appropriateTypes.Add(type);
            }

            return appropriateTypes.OrderBy(x => x.FullName);
        }

        public static Type GetManagedReferenceFieldType(this SerializedProperty property)
        {
            Type realPropertyType = GetRealTypeFromTypename(property.managedReferenceFieldTypename);
            if (realPropertyType != null)
                return realPropertyType;
            Debug.LogError($"Can not get field type of managed reference : {property.managedReferenceFieldTypename}");
            return null;
        }

        public static Type GetRealTypeFromTypename(string stringType)
        {
            (string, string) names = GetSplitNamesFromTypename(stringType);
            return Type.GetType($"{names.Item2}, {names.Item1}");
        }

        public static (string AssemblyName, string ClassName) GetSplitNamesFromTypename(string typename)
        {
            if (string.IsNullOrEmpty(typename))
                return ("", "");
            string[] typeSplitString = typename.Split(char.Parse(" "));
            string typeClassName = typeSplitString[1];
            string typeAssemblyName = typeSplitString[0];
            return (typeAssemblyName, typeClassName);
        }
    }
}