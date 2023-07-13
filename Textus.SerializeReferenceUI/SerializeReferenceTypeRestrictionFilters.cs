using System;
using System.Linq;

namespace SerializeReferenceUI
{
    public static class SerializeReferenceTypeRestrictionFilters
    {
        public static Func<Type, bool> TypeIsNotSubclassOrEqualOrHasInterface(Type[] types)
        {
            return type => !TypeIsSubclassOrEqualOrHasInterface(types)(type);
        }

        public static Func<Type, bool> TypeIsSubclassOrEqualOrHasInterface(Type[] types)
        {
            return type => types.Any(e => e.IsInterface ? type.TypeHasInterface(e) : TypeIsSubclassOrEqual(type, e));
        }

        public static bool TypeIsSubclassOrEqual(Type type, Type comparator) => type.IsSubclassOf(comparator) || type == comparator;
        public static bool TypeHasInterface(this Type type, Type comparator) => type.GetInterface(comparator.ToString()) != null;
    }
}