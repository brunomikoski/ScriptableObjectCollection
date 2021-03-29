using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomPropertyDrawer(typeof(CollectionItemIndirectReference), true)]
    public sealed class CollectionItemIndirectReferenceDrawer : PropertyDrawer
    {
        private const string OBJECT_ASSET_PROPERTY_PATH = "editorAsset";
        private const string COLLECTION_ITEM_GUID_PROPERTY_PATH = "collectionItemGUID";
        private const string COLLECTION_GUID_PROPERTY_PATh = "collectionGUID";


        private SerializedProperty objectAssetProperty;
        private SerializedProperty collectionItemGUIDProperty;
        private SerializedProperty collectionGUIDProperty;

        private ScriptableObjectCollectionItem cachedReference;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            objectAssetProperty = property.FindPropertyRelative(OBJECT_ASSET_PROPERTY_PATH);
            collectionItemGUIDProperty = property.FindPropertyRelative(COLLECTION_ITEM_GUID_PROPERTY_PATH);
            collectionGUIDProperty = property.FindPropertyRelative(COLLECTION_GUID_PROPERTY_PATh);

            if (objectAssetProperty.objectReferenceValue != null)
            {
                if (string.IsNullOrEmpty(collectionItemGUIDProperty.stringValue)
                    || string.IsNullOrEmpty(collectionGUIDProperty.stringValue))
                {
                    ScriptableObjectCollectionItem collectionItem = objectAssetProperty.objectReferenceValue as ScriptableObjectCollectionItem;
                    collectionItemGUIDProperty.stringValue = collectionItem.GUID;
                    collectionGUIDProperty.stringValue = collectionItem.Collection.GUID;
                    objectAssetProperty.serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(collectionItemGUIDProperty.stringValue)
                    && !string.IsNullOrEmpty(collectionGUIDProperty.stringValue))
                {
                    if (CollectionsRegistry.Instance.TryGetCollectionByGUID(collectionGUIDProperty.stringValue,
                        out ScriptableObjectCollection collection))
                    {
                        if (collection.TryGetItemByGUID(collectionItemGUIDProperty.stringValue,
                            out ScriptableObjectCollectionItem collectionItem))
                        {
                            objectAssetProperty.objectReferenceValue = collectionItem;
                            objectAssetProperty.serializedObject.ApplyModifiedProperties();
                        }
                    }
                }
            }

            if (cachedReference == null && objectAssetProperty.objectReferenceValue != null)
                cachedReference = objectAssetProperty.objectReferenceValue as ScriptableObjectCollectionItem;
            
            EditorGUI.PropertyField(position, objectAssetProperty, label, true);
            if (objectAssetProperty.objectReferenceValue != cachedReference)
            {
                string collectionItemGUID = string.Empty;
                string collectionGUID = string.Empty;
            
                if (objectAssetProperty.objectReferenceValue != null &&
                    objectAssetProperty.objectReferenceValue is ScriptableObjectCollectionItem collectionItem)
                {
                    collectionItemGUID = collectionItem.GUID;
                    collectionGUID = collectionItem.Collection.GUID;
                    cachedReference = collectionItem;
                }
                else
                {
                    cachedReference = null;
                }
            
                collectionItemGUIDProperty.stringValue = collectionItemGUID;
                collectionGUIDProperty.stringValue = collectionGUID;
                objectAssetProperty.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(objectAssetProperty.serializedObject.targetObject);
            }
        }

    }
}

