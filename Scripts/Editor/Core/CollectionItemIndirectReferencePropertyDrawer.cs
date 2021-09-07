using System;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomPropertyDrawer(typeof(CollectionItemIndirectReference), true)]
    public sealed class CollectionItemIndirectReferencePropertyDrawer : PropertyDrawer
    {
        private const string COLLECTION_ITEM_GUID_PROPERTY_PATH = "collectionItemGUID";
        private const string COLLECTION_GUID_PROPERTY_PATH = "collectionGUID";

        private Type collectionItemType;
        private CollectionItemPropertyDrawer collectionItemPropertyDrawer;

        private SerializedProperty drawingProperty;
        private SerializedProperty itemGUIDSerializedProperty;
        private SerializedProperty collectionGUIDSerializedProperty;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (collectionItemType == null)
                SetCollectionItemType();
            if (collectionItemPropertyDrawer == null) 
                CreateCollectionItemPropertyDrawer();

            drawingProperty = property;
            itemGUIDSerializedProperty = property.FindPropertyRelative(COLLECTION_ITEM_GUID_PROPERTY_PATH);
            collectionGUIDSerializedProperty = property.FindPropertyRelative(COLLECTION_GUID_PROPERTY_PATH);

            string itemGUID = itemGUIDSerializedProperty.stringValue;
            ScriptableObjectCollectionItem collectionItem = GetCollectionItem(itemGUID, collectionGUIDSerializedProperty.stringValue);

            int indexOfArrayPart = property.propertyPath.IndexOf('[');
            if (indexOfArrayPart > -1)
            {
                if (string.Equals(label.text, itemGUID, StringComparison.Ordinal))
                {
                    label.text = $"Element {property.propertyPath.Substring(indexOfArrayPart + 1, 1)}";
                }
            }

            if (collectionItemPropertyDrawer.OptionsAttribute.DrawType == DrawType.Dropdown)
            {
                DrawItemDrawer(position, label, collectionItem);
                return;
            }

            EditorGUI.PropertyField(position, property, label, true);
        }

        private void DrawItemDrawer(Rect position, GUIContent label, ScriptableObjectCollectionItem collectionItem
        )
        {
            collectionItemPropertyDrawer.DrawCollectionItemDrawer(ref position, collectionItem, label, item =>
            {
                SetSerializedPropertyGUIDs(item);
                drawingProperty.serializedObject.ApplyModifiedProperties();
            });
        }

        private void SetSerializedPropertyGUIDs(ScriptableObjectCollectionItem item)
        {
            if (item == null)
            {
                itemGUIDSerializedProperty.stringValue = string.Empty;
                collectionGUIDSerializedProperty.stringValue = string.Empty;
            }
            else
            {
                itemGUIDSerializedProperty.stringValue = item.GUID;
                collectionGUIDSerializedProperty.stringValue = item.Collection.GUID;
            }
        }

        private static ScriptableObjectCollectionItem GetCollectionItem(string itemGUID, string collectionGUID)
        {
            if (string.IsNullOrEmpty(itemGUID)) 
                return null;
            
            if (string.IsNullOrEmpty(collectionGUID)) 
                return null;
            
            if (!CollectionsRegistry.Instance.TryGetCollectionByGUID(collectionGUID, out ScriptableObjectCollection collection))
                return null;
            
            if (!collection.TryGetItemByGUID(itemGUID, out ScriptableObjectCollectionItem resultCollection))
                return null;
            return resultCollection;
        }

        private void CreateCollectionItemPropertyDrawer()
        {
            collectionItemPropertyDrawer = new CollectionItemPropertyDrawer();
            collectionItemPropertyDrawer.Initialize(collectionItemType, GetOptionsAttribute());
        }

        private void SetCollectionItemType()
        {
            Type arrayOrListType = fieldInfo.FieldType.GetArrayOrListType();
            Type properFieldType = arrayOrListType ?? fieldInfo.FieldType;
            collectionItemType = GetGenericItemType(properFieldType).GetGenericArguments()[0];
        }

        private Type GetGenericItemType(Type targetType)
        {
            Type baseType = targetType.BaseType;

            while (baseType != null)
            {
                if (baseType.IsGenericType &&
                    baseType.GetGenericTypeDefinition() == typeof(CollectionItemIndirectReference<>))
                    return baseType;
                baseType = baseType.BaseType;
            }

            return null;
        }

        private CollectionItemEditorOptionsAttribute GetOptionsAttribute()
        {
            if (fieldInfo == null)
                return null;
            object[] attributes = fieldInfo.GetCustomAttributes(typeof(CollectionItemEditorOptionsAttribute), false);
            if (attributes.Length > 0)
                return attributes[0] as CollectionItemEditorOptionsAttribute;
            return null;
        }
    }
}
