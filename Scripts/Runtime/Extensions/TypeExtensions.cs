using System;
using System.Collections.Generic;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class TypeExtensions
    {
        private static bool IsList(this Type type)
        {
            if (type.IsArray)
                return false;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
                return true;

            return false;
        }

        public static Type GetArrayOrListType(this Type type)
        {
            if (type.IsArray)
                return type.GetElementType();

            if (type.IsList())
                return type.GetGenericArguments()[0];

            return null;
        }
    }
}
