using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static partial class ReflectionUtility
    {
        private static Dictionary<Type, List<FieldInfo>> typeToModifiableFieldsCache = new Dictionary<Type, List<FieldInfo>>();
        private static Dictionary<string, Type> managedReferenceFullTypeNameToTypeCache = new Dictionary<string, Type>();

        public static Type GetTypeFromManagedFullTypeName(string targetManagedFullTypeName, bool ignoreCache = false)
        {
            if (!ignoreCache && managedReferenceFullTypeNameToTypeCache.TryGetValue(targetManagedFullTypeName, out Type type))
                return type;
            
            string[] typeInfo = targetManagedFullTypeName.Split(' ');
            type = Type.GetType($"{typeInfo[1]}, {typeInfo[0]}");
            managedReferenceFullTypeNameToTypeCache.Add(targetManagedFullTypeName, type);

            return type;
        }
        
        public static List<FieldInfo> GetModifiableFields(Type targetType, bool recursive = true, bool ignoreCache = false)
        {
            if (!ignoreCache && typeToModifiableFieldsCache.TryGetValue(targetType, out List<FieldInfo> result))
                return result;

            result = new List<FieldInfo>();
            FieldInfo[] fis = targetType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < fis.Length; i++)
            {
                FieldInfo info = fis[i];
                if (info.IsNotSerialized)
                    continue;

                result.Add(info);
            }
            
            typeToModifiableFieldsCache.Add(targetType, result);
            return result;
        }
        
        public static void CopyFields(Object src, object dst, BindingFlags bindingAttr = BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance)
        {
            if (src != null && dst != null)
            {
                Type type = src.GetType();
                FieldInfo[] fields = type.GetFields(bindingAttr);
                for (int i = 0; i < fields.Length; ++i)
                    if (!fields[i].IsStatic)
                        fields[i].SetValue(dst, fields[i].GetValue(src));
            }
        }

        public static IEnumerable<Type> GetTypesInAssembly(Assembly assembly, Predicate<Type> predicate)
        {
            if (assembly == null)
                return null;

            Type[] types = new Type[0];
            try
            {
                types = assembly.GetTypes();
            }
            catch (Exception)
            {
                // Can't load the types in this assembly
            }
            types = (from t in types
                where t != null && predicate(t)
                select t).ToArray();
            return types;
        }

        public static T AccessInternalField<T>(this Type type, object obj, string memberName)
        {
            if (string.IsNullOrEmpty(memberName) || (type == null))
                return default(T);

            BindingFlags bindingFlags = BindingFlags.NonPublic;
            if (obj != null)
                bindingFlags |= BindingFlags.Instance;
            else
                bindingFlags |= BindingFlags.Static;

            FieldInfo field = type.GetField(memberName, bindingFlags);
            if ((field != null) && (field.FieldType == typeof(T)))
                return (T)field.GetValue(obj);
            
            return default(T);
        }
        

        public static object GetParentObject(string path, object obj)
        {
            string[] fields = path.Split('.');
            if (fields.Length == 1)
                return obj;

            FieldInfo info = obj.GetType().GetField(
                fields[0], BindingFlags.Public 
                           | BindingFlags.NonPublic 
                           | BindingFlags.Instance);
            obj = info.GetValue(obj);

            return GetParentObject(string.Join(".", fields, 1, fields.Length - 1), obj);
        }

        public static string GetFieldPath<TType, TValue>(Expression<Func<TType, TValue>> expr)
        {
            MemberExpression me;
            switch (expr.Body.NodeType)
            {
                case ExpressionType.MemberAccess:
                    me = expr.Body as MemberExpression;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            List<string> members = new List<string>();
            while (me != null)
            {
                members.Add(me.Member.Name);
                me = me.Expression as MemberExpression;
            }

            StringBuilder sb = new StringBuilder();
            for (int i = members.Count - 1; i >= 0; i--)
            {
                sb.Append(members[i]);
                if (i > 0) sb.Append('.');
            }
            return sb.ToString();
        }
    }
}