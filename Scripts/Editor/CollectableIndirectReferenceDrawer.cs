using BrunoMikoski.ScriptableObjectCollections.Core;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomPropertyDrawer(typeof(CollectableIndirectReference), true)]
    public sealed class CollectableIndirectReferenceDrawer : PropertyDrawer
    {
        private const string EDITOR_ASSET_PROPERTY_PATH = "editorAsset";
        private const string COLLECTABLE_GUID_PROPERTY_PATH = "collectableGUID";
        private const string COLLECTION_GUID_PROPERTY_PATh = "collectionGUID";

        
        private SerializedProperty editorAssetproperty;
        private SerializedProperty collectableGUIDProperty;
        private SerializedProperty collectionGUIDProperty;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            editorAssetproperty = property.FindPropertyRelative(EDITOR_ASSET_PROPERTY_PATH);
            collectableGUIDProperty = property.FindPropertyRelative(COLLECTABLE_GUID_PROPERTY_PATH);
            collectionGUIDProperty = property.FindPropertyRelative(COLLECTION_GUID_PROPERTY_PATh);

            using (EditorGUI.ChangeCheckScope scope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUI.PropertyField(position, editorAssetproperty, label, true);

                if (scope.changed)
                {
                    string collectableGUID = string.Empty;
                    string collectionGUID = string.Empty;
                        
                    if (editorAssetproperty.objectReferenceValue != null && editorAssetproperty.objectReferenceValue is CollectableScriptableObject collectable)
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
