using System;
using System.Reflection;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class SerializedPropertyUtility
    {
        private static FieldInfo GetFieldViaPath(Type parentType, string path)
        {
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
