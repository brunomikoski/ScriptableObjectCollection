using UnityEditor;

namespace BrunoMikoski.ScriptableObjectCollections 
{
    public static class SerializedPropertyExtension
    {
        public static SerializedProperty DeepFindPropertyRelative(this SerializedProperty property, string relativePropertyPath) 
        {
            SerializedProperty relativeProperty = property.FindPropertyRelative(relativePropertyPath);

            if (relativeProperty == null) 
            {
                relativeProperty = property.FindPropertyRelative($"<{relativePropertyPath}>k__BackingField");
            }

            return relativeProperty;
        }
    }
}