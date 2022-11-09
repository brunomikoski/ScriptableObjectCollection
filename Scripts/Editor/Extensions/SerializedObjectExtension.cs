using UnityEditor;

namespace BrunoMikoski.ScriptableObjectCollections 
{
    public static class SerializedObjectExtension 
    {
        public static SerializedProperty DeepFindProperty(this SerializedObject @object, string propertyPath) 
        {
            SerializedProperty property = @object.FindProperty(propertyPath);

            if (property == null) 
            {
                property = @object.FindProperty($"<{propertyPath}>k__BackingField");
            }

            return property;
        }
    }
}