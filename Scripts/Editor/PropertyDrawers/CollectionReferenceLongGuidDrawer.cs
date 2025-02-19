using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Picker
{
    [CustomPropertyDrawer(typeof(CollectionReferenceLongGuidAttribute))]
    public class CollectionReferenceLongGuidDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            LongGuid collectionGUID = (LongGuid)property.boxedValue;

            ScriptableObjectCollection collection = null;
            if (collectionGUID.IsValid())
            {
                collection = CollectionsRegistry.Instance.GetCollectionByGUID(collectionGUID);
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.ObjectField(position, "Collection", collection, typeof(ScriptableObjectCollection), false);
            EditorGUI.EndDisabledGroup();
        }
    }
}