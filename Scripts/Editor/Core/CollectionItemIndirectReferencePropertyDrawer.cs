using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomPropertyDrawer(typeof(CollectionItemIndirectReference), true)]
    public sealed class CollectionItemIndirectReferencePropertyDrawer : PropertyDrawer
    {
        private const string COLLECTION_ITEM_GUID_VALUE_A_PROPERTY_PATH = "collectionItemGUIDValueA";
        private const string COLLECTION_ITEM_GUID_VALUE_B_PROPERTY_PATH = "collectionItemGUIDValueB";
        private const string COLLECTION_GUID_VALUE_A_PROPERTY_PATH = "collectionGUIDValueA";
        private const string COLLECTION_GUID_VALUE_B_PROPERTY_PATH = "collectionGUIDValueB";

        private Type collectionItemType;
        private CollectionItemPropertyDrawer collectionItemPropertyDrawer;

        private SerializedProperty drawingProperty;
        private SerializedProperty itemGUIDValueASerializedProperty;
        private SerializedProperty itemGUIDValueBSerializedProperty;
        private SerializedProperty collectionGUIDValueASerializedProperty;
        private SerializedProperty collectionGUIDValueBSerializedProperty;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (collectionItemType == null)
                SetCollectionItemType();
            
            if (collectionItemPropertyDrawer == null) 
                CreateCollectionItemPropertyDrawer(property.serializedObject.targetObject);

            drawingProperty = property;
            itemGUIDValueASerializedProperty = property.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_A_PROPERTY_PATH);
            itemGUIDValueBSerializedProperty = property.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_B_PROPERTY_PATH);
            collectionGUIDValueASerializedProperty = property.FindPropertyRelative(COLLECTION_GUID_VALUE_A_PROPERTY_PATH);
            collectionGUIDValueBSerializedProperty = property.FindPropertyRelative(COLLECTION_GUID_VALUE_B_PROPERTY_PATH);

            TryGetCollectionItem(out ScriptableObject collectionItem);
            
            int indexOfArrayPart = property.propertyPath.IndexOf('[');
            if (indexOfArrayPart > -1)
            {
                if (string.Equals(label.text, itemGUIDValueASerializedProperty.ulongValue.ToString(), StringComparison.Ordinal))
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

        private void DrawItemDrawer(Rect position, GUIContent label, ScriptableObject collectionItem
        )
        {
            collectionItemPropertyDrawer.DrawCollectionItemDrawer(ref position, collectionItem, label, item =>
            {
                SetSerializedPropertyGUIDs(item);
                drawingProperty.serializedObject.ApplyModifiedProperties();
            });
        }

        private void SetSerializedPropertyGUIDs(ScriptableObject item)
        {
            if (item == null)
            {
                itemGUIDValueASerializedProperty.ulongValue = 0;
                itemGUIDValueBSerializedProperty.ulongValue = 0;
                collectionGUIDValueASerializedProperty.ulongValue = 0;
                collectionGUIDValueBSerializedProperty.ulongValue = 0;
                
            }
            else
            {
                if (item is ISOCItem socItem)
                {
                    (ulong, ulong) itemGUIDValues = socItem.GUID.GetValue();
                    itemGUIDValueASerializedProperty.ulongValue = itemGUIDValues.Item1;
                    itemGUIDValueBSerializedProperty.ulongValue = itemGUIDValues.Item2;

                    (ulong, ulong) collectionGUIDValues = socItem.Collection.GUID.GetValue();
                    collectionGUIDValueASerializedProperty.ulongValue = collectionGUIDValues.Item1;
                    collectionGUIDValueBSerializedProperty.ulongValue = collectionGUIDValues.Item2;
                }
            }
        }

        private bool TryGetCollectionItem(out ScriptableObject item)
        {
            item = null;

            if (itemGUIDValueASerializedProperty.ulongValue == 0 ||
                itemGUIDValueBSerializedProperty.ulongValue == 0 ||
                collectionGUIDValueASerializedProperty.ulongValue == 0 ||
                collectionGUIDValueBSerializedProperty.ulongValue == 0)
                return false;
            
            ULongGuid itemGUID = new ULongGuid(itemGUIDValueASerializedProperty.ulongValue, itemGUIDValueBSerializedProperty.ulongValue);
            ULongGuid collectionGUID = new ULongGuid(collectionGUIDValueASerializedProperty.ulongValue, collectionGUIDValueBSerializedProperty.ulongValue);
            
            if (!CollectionsRegistry.Instance.TryGetCollectionByGUID(collectionGUID, out ScriptableObjectCollection collection))
                return false;
            
            if (!collection.TryGetItemByGUID(itemGUID, out ScriptableObject resultItem))
                return false;

            item = resultItem;
            return true;
        }

        private void CreateCollectionItemPropertyDrawer(Object serializedObjectTargetObject)
        {
            collectionItemPropertyDrawer = new CollectionItemPropertyDrawer();
            collectionItemPropertyDrawer.Initialize(collectionItemType, serializedObjectTargetObject,
                GetOptionsAttribute());
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

            return new CollectionItemEditorOptionsAttribute();
        }
    }
}
