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

        private CollectableScriptableObject collectableScriptableObject;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            objectAssetProperty = property.FindPropertyRelative(OBJECT_ASSET_PROPERTY_PATH);
            collectableGUIDProperty = property.FindPropertyRelative(COLLECTABLE_GUID_PROPERTY_PATH);
            collectionGUIDProperty = property.FindPropertyRelative(COLLECTION_GUID_PROPERTY_PATh);

            if (objectAssetProperty.exposedReferenceValue == null
                && !string.IsNullOrEmpty(collectableGUIDProperty.stringValue)
                && !string.IsNullOrEmpty(collectionGUIDProperty.stringValue))
            {
                if (CollectionsRegistry.Instance.TryGetCollectionByGUID(collectionGUIDProperty.stringValue,
                    out ScriptableObjectCollection collection))
                {
                    if (collection.TryGetCollectableByGUID(collectableGUIDProperty.stringValue,
                        out CollectableScriptableObject collectable))
                    {
                        objectAssetProperty.objectReferenceValue = collectable;
                    }
                }
            }
            
            using (EditorGUI.ChangeCheckScope scope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUI.PropertyField(position, objectAssetProperty, label, true);

                if (scope.changed)
                {
                    string collectableGUID = string.Empty;
                    string collectionGUID = string.Empty;
                        
                    if (objectAssetProperty.objectReferenceValue != null && objectAssetProperty.objectReferenceValue is CollectableScriptableObject collectable)
                    {
                        collectableGUID = collectable.GUID;
                        collectionGUID = collectable.Collection.GUID;
                    }
                    collectableGUIDProperty.stringValue = collectableGUID;
                    collectionGUIDProperty.stringValue = collectionGUID;
                }
            }
        }
    }
}
