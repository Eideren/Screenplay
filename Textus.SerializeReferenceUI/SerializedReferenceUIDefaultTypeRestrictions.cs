using System;
using System.Collections.Generic;
using System.Reflection;

namespace SerializeReferenceUI
{
    public static class SerializedReferenceUIDefaultTypeRestrictions
    {
        public static IEnumerable<Func<Type, bool>> GetAllBuiltInTypeRestrictions(FieldInfo fieldInfo)
        {
            var result = new List<Func<Type, bool>>();
            object[] attributeObjects = fieldInfo.GetCustomAttributes(false);
            foreach (object attributeObject in attributeObjects)
            {
                if (attributeObject is not SerializeReferenceUIRestrictionIncludeTypes includeTypes)
                {
                    if (attributeObject is SerializeReferenceUIRestrictionExcludeTypes excludeTypes)
                        result.Add(SerializeReferenceTypeRestrictionFilters.TypeIsNotSubclassOrEqualOrHasInterface(excludeTypes.Types));
                }
                else
                    result.Add(SerializeReferenceTypeRestrictionFilters.TypeIsSubclassOrEqualOrHasInterface(includeTypes.Types));
            }

            return result;
        }
    }
}