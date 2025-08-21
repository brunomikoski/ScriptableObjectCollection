using BrunoMikoski.ScriptableObjectCollections.Utils;
using UnityEditor;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class SerializedObjectExtensions
    {
        public static bool TryFindProperty(this SerializedObject serializedObject, string propertyName, out SerializedProperty property, bool tryBackingFieldIfFails = true)
        {
            property = serializedObject.FindProperty(propertyName);
            if (property == null && tryBackingFieldIfFails)
            {
                property = serializedObject.FindProperty(propertyName.AsBackingField());
            }

            return property != null;
        }
    }
}