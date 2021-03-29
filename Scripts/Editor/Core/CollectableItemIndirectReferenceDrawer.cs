using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomPropertyDrawer(typeof(CollectionItemIndirectReference), true)]
    public sealed class CollectableItemIndirectReferenceDrawer : PropertyDrawer
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
                    ScriptableObjectCollectionItem collectable = objectAssetProperty.objectReferenceValue as ScriptableObjectCollectionItem;
                    collectionItemGUIDProperty.stringValue = collectable.GUID;
                    collectionGUIDProperty.stringValue = collectable.Collection.GUID;
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
                        if (collection.TryGetCollectableByGUID(collectionItemGUIDProperty.stringValue,
                            out ScriptableObjectCollectionItem collectable))
                        {
                            objectAssetProperty.objectReferenceValue = collectable;
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
                string collectableGUID = string.Empty;
                string collectionGUID = string.Empty;
            
                if (objectAssetProperty.objectReferenceValue != null &&
                    objectAssetProperty.objectReferenceValue is ScriptableObjectCollectionItem collectable)
                {
                    collectableGUID = collectable.GUID;
                    collectionGUID = collectable.Collection.GUID;
                    cachedReference = collectable;
                }
                else
                {
                    cachedReference = null;
                }
            
                collectionItemGUIDProperty.stringValue = collectableGUID;
                collectionGUIDProperty.stringValue = collectionGUID;
                objectAssetProperty.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(objectAssetProperty.serializedObject.targetObject);
            }
        }

    }
}

