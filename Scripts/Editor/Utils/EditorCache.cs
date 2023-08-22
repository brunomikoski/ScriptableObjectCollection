using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class EditorCache
    {
        private static Dictionary<object, Editor> typeToEditorCache = new();

        private static Dictionary<Type, bool> typeToHasCustomEditorCache = new();
        
        public static Editor GetOrCreateEditorForObject(Object targetObject)
        {
            if (typeToEditorCache.TryGetValue(targetObject, out Editor editor))
                return editor;
            
            editor = Editor.CreateEditor(targetObject);
            typeToEditorCache.Add(targetObject, editor);
            return editor;
        }

        public static bool HasCustomEditor(Object objectReferenceValue)
        {
            Type objectType = objectReferenceValue.GetType();
            if(typeToHasCustomEditorCache.TryGetValue(objectType, out bool hasCustomEditor))
                return hasCustomEditor;

            TypeCache.TypeCollection customEditors = TypeCache.GetTypesWithAttribute<CustomEditor>();
            
            foreach (var type in customEditors)
            {
                object[] attributes = type.GetCustomAttributes(typeof(CustomEditor), true);
                foreach (var attribute in attributes)
                {
                    Type attributeType = attribute.GetType();
                    
                    // Access the `m_InspectedType` field using reflection
                    FieldInfo fieldInfo = attributeType.GetField("m_InspectedType", BindingFlags.NonPublic | BindingFlags.Instance);

                    if (fieldInfo != null)
                    {
                        Type inspectedType = fieldInfo.GetValue(attribute) as Type;

                        if (inspectedType == objectType || inspectedType.IsSubclassOf(objectType))
                        {
                            typeToHasCustomEditorCache.Add(objectType, true);
                            return true;
                        }
                    }
                }
            }
            typeToHasCustomEditorCache.Add(objectType, false);
            return false;
        }
    }
}