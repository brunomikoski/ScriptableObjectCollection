using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomPropertyDrawer(typeof(SerializableDictionary<,>))]
    public class GenericDictionaryPropertyDrawer : PropertyDrawer
    {
        private static readonly float LineHeight = EditorGUIUtility.singleLineHeight;
        private static readonly float VertSpace = EditorGUIUtility.standardVerticalSpacing;
        private const float WARNING_BOX_HEIGHT = 1.5f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Draw list of key/value pairs.
            SerializedProperty list = property.FindPropertyRelative("list");
            EditorGUI.PropertyField(position, list, label, true);

            // Draw key collision warning.
            bool keyCollision = property.FindPropertyRelative("keyCollision").boolValue;
            if (keyCollision)
            {
                position.y += EditorGUI.GetPropertyHeight(list, true);
                if (!list.isExpanded)
                {
                    position.y += VertSpace;
                }
                position.height = LineHeight * WARNING_BOX_HEIGHT;
                position = EditorGUI.IndentedRect(position);
                EditorGUI.HelpBox(position, "Duplicate keys will not be serialized.", MessageType.Warning);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Height of KeyValue list.
            float height = 0f;
            SerializedProperty list = property.FindPropertyRelative("list");
            height += EditorGUI.GetPropertyHeight(list, true);

            // Height of key collision warning.
            bool keyCollision = property.FindPropertyRelative("keyCollision").boolValue;
            if (keyCollision)
            {
                height += WARNING_BOX_HEIGHT * LineHeight;
                if (!list.isExpanded)
                {
                    height += VertSpace;
                }
            }
            return height;
        }
    }
}
