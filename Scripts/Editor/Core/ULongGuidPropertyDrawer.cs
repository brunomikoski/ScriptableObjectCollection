using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomPropertyDrawer(typeof(LongGuid))]
    public class ULongGuidPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty value1Property = property.FindPropertyRelative("value1");
            SerializedProperty value2Property = property.FindPropertyRelative("value2");
            long value1 = value1Property.longValue;
            long value2 = value2Property.longValue;

            LongGuid guid = new LongGuid(value1, value2);
            string guidString = guid.ToString();

            EditorGUI.BeginDisabledGroup(true);

            EditorGUI.TextField(position, label.text, guidString);
            EditorGUI.EndDisabledGroup();
        }
    }
}