using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomPropertyDrawer(typeof(CollectionItemIndirectReference), true)]
    public sealed class CollectionItemIndirectReferencePropertyDrawer : PropertyDrawer
    {
        private const string COLLECTION_ITEM_GUID_PROPERTY_PATH = "collectionItemGUID";
        private const string COLLECTION_GUID_PROPERTY_PATh = "collectionGUID";

        private SerializedProperty collectionItemGUIDSerializedProperty;
        private SerializedProperty collectionGUIDSerializedProperty;
        private CollectionItemItemPropertyDrawer collectionItemPropertyDrawer;

        private ScriptableObjectCollectionItem collectionItem;
        private Type collectionItemType;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (collectionItemType == null)
            {
                Type arrayOrListType = fieldInfo.FieldType.GetArrayOrListType();
                Type properFieldType = arrayOrListType != null ? arrayOrListType : fieldInfo.FieldType;
                collectionItemType = GetGenericItemType(properFieldType).GetGenericArguments().First();
            }

            if (collectionItemPropertyDrawer == null)
            {
                collectionItemPropertyDrawer = new CollectionItemItemPropertyDrawer();
                collectionItemPropertyDrawer.Initialize(collectionItemType, property.serializedObject.targetObject);
            }
            
            
            collectionItemGUIDSerializedProperty = property.FindPropertyRelative(COLLECTION_ITEM_GUID_PROPERTY_PATH);
            collectionGUIDSerializedProperty = property.FindPropertyRelative(COLLECTION_GUID_PROPERTY_PATh);

            if (collectionItem != null)
            {
                if (string.IsNullOrEmpty(collectionItemGUIDSerializedProperty.stringValue)
                    || string.IsNullOrEmpty(collectionGUIDSerializedProperty.stringValue))
                {
                    collectionItemGUIDSerializedProperty.stringValue = collectionItem.GUID;
                    collectionGUIDSerializedProperty.stringValue = collectionItem.Collection.GUID;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(collectionItemGUIDSerializedProperty.stringValue)
                    && !string.IsNullOrEmpty(collectionGUIDSerializedProperty.stringValue))
                {
                    if (CollectionsRegistry.Instance.TryGetCollectionByGUID(collectionGUIDSerializedProperty.stringValue,
                        out ScriptableObjectCollection collection))
                    {
                        if (collection.TryGetItemByGUID(collectionItemGUIDSerializedProperty.stringValue,
                            out ScriptableObjectCollectionItem resultCollection))
                        {
                            collectionItem = resultCollection;
                        }
                    }
                }
            }

            collectionItemPropertyDrawer.DrawCollectionItemDrawer(
                position, collectionItem, label,
                item =>
                {
                    string collectionItemGUID = string.Empty;
                    string collectionGUID = string.Empty;
                    if (item != null)
                    {
                        collectionItemGUID = item.GUID;
                        collectionGUID = item.Collection.GUID;
                    }
                    
                    collectionItemGUIDSerializedProperty.stringValue = collectionItemGUID;
                    collectionGUIDSerializedProperty.stringValue = collectionGUID;
                    collectionItem = item;
                    property.serializedObject.ApplyModifiedProperties();
                }
            );
        }

        private Type GetGenericItemType(Type targetType)
        {
            Type baseType = targetType.BaseType;

            while (baseType != null)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(CollectionItemIndirectReference<>))
                    return baseType;
                baseType = baseType.BaseType;
            }
            return null;
        }
    }
}

