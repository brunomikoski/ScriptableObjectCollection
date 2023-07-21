using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static partial class TypeExtensions
    {
        private static bool IsList(this Type type)
        {
            if (type.IsArray)
                return false;

            Type[] interfaces = type.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                Type @interface = interfaces[i];
                if (@interface.IsGenericType)
                {
                    if (@interface.GetGenericTypeDefinition() == typeof(ICollection<>))
                        return true;
                }
            }
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

        public static Type GetBaseGenericType(this Type type)
        {
            Type baseType = type.BaseType;

            while (baseType != null)
            {
                if (baseType.IsGenericType &&
                    baseType.GetGenericTypeDefinition() == typeof(CollectionItemIndirectReference<>))
                    return baseType;
                baseType = baseType.BaseType;
            }

            return null;
        }
    }
}
