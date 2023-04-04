using System;
using System.Reflection;
using UnityEditor;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class SerializedPropertyExtensions
    {
        public static FieldInfo GetFieldInfoFromPathByType(this SerializedProperty property, Type parentType)
        {
            string path = property.propertyPath;
            FieldInfo fieldInfo = parentType.GetField(path);
            string[] perDot = path.Split('.');
            foreach (string fieldName in perDot)
            {
                fieldInfo = parentType.GetField(fieldName);
                if (fieldInfo == null) 
                    return null;
                
                parentType = fieldInfo.FieldType;
            }
            
            return fieldInfo;
        }
    }
}
