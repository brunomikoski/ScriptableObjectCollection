using System;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomPropertyDrawer(typeof(ScriptableObjectReferenceItem))]
    public sealed class ScriptableObjectReferenceItemEditor : PropertyDrawer
    {
        private SerializedProperty itemGuidSerializedProperty;
        bool showTargetItem = true;
        private ScriptableObjectCollectionItem item;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            itemGuidSerializedProperty = property.FindPropertyRelative(nameof(ScriptableObjectReferenceItem.targetGuid));
            if (item == null && !String.IsNullOrEmpty(itemGuidSerializedProperty.stringValue))
            {
                item = AssetDatabase.LoadAssetAtPath<ScriptableObjectCollectionItem>(
                    AssetDatabase.GUIDToAssetPath(itemGuidSerializedProperty.stringValue));
            }
            property.serializedObject.Update();
            
            EditorGUI.BeginProperty(position, label, property);
            Rect rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            showTargetItem = EditorGUI.Foldout(rect, showTargetItem, "Target Item");
            if (showTargetItem)
            {             
                EditorGUI.indentLevel++;
                ShowTargetItem(position);
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndProperty();
            EditorUtility.SetDirty(property.serializedObject.targetObject);
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (showTargetItem)
            {
                return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
            }
            return EditorGUIUtility.singleLineHeight;
        }
        
        private void ShowTargetItem(Rect position)
        {
            Rect rect = new Rect(position);
            rect.y += EditorGUIUtility.singleLineHeight;
            rect.height = EditorGUIUtility.singleLineHeight;
            Type itemType = typeof(ScriptableObjectCollectionItem);
            if (item != null)
                itemType = item.GetType();
            UnityEngine.Object objectSetByUser = EditorGUI.ObjectField(rect, item, itemType, false);
            if (objectSetByUser is ScriptableObjectCollectionItem collectionItem && collectionItem.IsReference())
            {
                Debug.LogError($"Cannot reference another reference object <{collectionItem.name}>!");
            }
            else if (objectSetByUser != item)
            {
                if (objectSetByUser == null)
                {
                    item = null;
                    itemGuidSerializedProperty.stringValue = string.Empty;
                    itemGuidSerializedProperty.serializedObject.ApplyModifiedProperties();
                }
                else if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(objectSetByUser, out string newGuid,
                             out long _))
                {
                    item = AssetDatabase.LoadAssetAtPath<ScriptableObjectCollectionItem>(
                        AssetDatabase.GUIDToAssetPath(newGuid));
                    itemGuidSerializedProperty.stringValue = newGuid;
                    itemGuidSerializedProperty.serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}
