#if !UNITY_2022_2_OR_NEWER
using System;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomPropertyDrawer(typeof(DrawAsSOCItemAttribute))]
    public class DrawAsSOCItemAttributePropertyDrawer : PropertyDrawer
    {
        private CollectionItemPropertyDrawer socItemPropertyDrawer;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!typeof(ISOCItem).IsAssignableFrom(fieldInfo.FieldType))
                throw new Exception("[DrawAsSOCItem] should only be used on ScriptableObjects that implements the ISOCItem interface");
            
            if (socItemPropertyDrawer == null)
            {
                socItemPropertyDrawer = new CollectionItemPropertyDrawer();
                socItemPropertyDrawer.OverrideFieldInfo(fieldInfo);
            }

            socItemPropertyDrawer.OnGUI(position, property, label);
        }
    }
}
#endif
