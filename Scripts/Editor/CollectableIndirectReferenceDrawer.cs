using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomPropertyDrawer(typeof(CollectableIndirectReference), true)]
    public sealed class CollectableIndirectReferenceDrawer : PropertyDrawer
    {
        private const string OBJECT_ASSET_PROPERTY_PATH = "editorAsset";
        private const string COLLECTABLE_GUID_PROPERTY_PATH = "collectableGUID";
        private const string COLLECTION_GUID_PROPERTY_PATh = "collectionGUID";


        private SerializedProperty objectAssetProperty;
        private SerializedProperty collectableGUIDProperty;
        private SerializedProperty collectionGUIDProperty;

        private CollectableScriptableObject cachedReference;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            objectAssetProperty = property.FindPropertyRelative(OBJECT_ASSET_PROPERTY_PATH);
            collectableGUIDProperty = property.FindPropertyRelative(COLLECTABLE_GUID_PROPERTY_PATH);
            collectionGUIDProperty = property.FindPropertyRelative(COLLECTION_GUID_PROPERTY_PATh);

            if (objectAssetProperty.objectReferenceValue != null)
            {
                if (string.IsNullOrEmpty(collectableGUIDProperty.stringValue)
                    || string.IsNullOrEmpty(collectionGUIDProperty.stringValue))
                {
                    CollectableScriptableObject collectable = objectAssetProperty.objectReferenceValue as CollectableScriptableObject;
                    collectableGUIDProperty.stringValue = collectable.GUID;
                    collectionGUIDProperty.stringValue = collectable.Collection.GUID;
                    objectAssetProperty.serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(collectableGUIDProperty.stringValue)
                    && !string.IsNullOrEmpty(collectionGUIDProperty.stringValue))
                {
                    if (CollectionsRegistry.Instance.TryGetCollectionByGUID(collectionGUIDProperty.stringValue,
                        out ScriptableObjectCollection collection))
                    {
                        if (collection.TryGetCollectableByGUID(collectableGUIDProperty.stringValue,
                            out CollectableScriptableObject collectable))
                        {
                            objectAssetProperty.objectReferenceValue = collectable;
                            objectAssetProperty.serializedObject.ApplyModifiedProperties();
                        }
                    }
                }
            }

            if (cachedReference == null && objectAssetProperty.objectReferenceValue != null)
                cachedReference = objectAssetProperty.objectReferenceValue as CollectableScriptableObject;
            
            EditorGUI.PropertyField(position, objectAssetProperty, label, true);
            if (objectAssetProperty.objectReferenceValue != cachedReference)
            {
                string collectableGUID = string.Empty;
                string collectionGUID = string.Empty;
            
                if (objectAssetProperty.objectReferenceValue != null &&
                    objectAssetProperty.objectReferenceValue is CollectableScriptableObject collectable)
                {
                    collectableGUID = collectable.GUID;
                    collectionGUID = collectable.Collection.GUID;
                    cachedReference = collectable;
                }
                else
                {
                    cachedReference = null;
                }
            
                collectableGUIDProperty.stringValue = collectableGUID;
                collectionGUIDProperty.stringValue = collectionGUID;
                objectAssetProperty.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(objectAssetProperty.serializedObject.targetObject);
            }
        }

    }
}

