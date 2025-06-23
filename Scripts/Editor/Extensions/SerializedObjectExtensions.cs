using UnityEditor;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class SerializedObjectExtensions
    {
        public static bool TryFindSerializedPropertyByName(this SerializedObject serializedObject, string propertyName, out SerializedProperty resultProperty, bool tryBackingField = true)
        {
            resultProperty = serializedObject.FindProperty(propertyName);
            if (resultProperty == null && tryBackingField)
            {
                //Try one more time using the backing field name.
                resultProperty = serializedObject.FindProperty(propertyName.AsBackingField());
            }

            return resultProperty != null;
        }
        
    }
}
