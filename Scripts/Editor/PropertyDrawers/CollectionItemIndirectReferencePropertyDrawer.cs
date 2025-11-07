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
        private const string COLLECTION_ITEM_LAST_KNOW_NAME_PROPERTY_PATH = "itemLastKnownName";
        private const string COLLECTION_GUID_VALUE_A_PROPERTY_PATH = "collectionGUIDValueA";
        private const string COLLECTION_GUID_VALUE_B_PROPERTY_PATH = "collectionGUIDValueB";
        private const string COLLECTION_LAST_KNOW_NAME_PROPERTY_PATH = "collectionLastKnowName";
        
        private Type collectionItemType;
        private CollectionItemPropertyDrawer collectionItemPropertyDrawer;
        
        private SerializedProperty itemGUIDValueASerializedProperty;
        private SerializedProperty itemGUIDValueBSerializedProperty;
        private SerializedProperty itemLastKnowNameSerializedProperty;
        private SerializedProperty collectionGUIDValueASerializedProperty;
        private SerializedProperty collectionGUIDValueBSerializedProperty;
        private SerializedProperty collectionLastKnowNameSerializedProperty;


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (collectionItemPropertyDrawer == null)
                return base.GetPropertyHeight(property, label);

            return collectionItemPropertyDrawer.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (collectionItemType == null)
                SetCollectionItemType();
            
            if (collectionItemPropertyDrawer == null) 
                CreateCollectionItemPropertyDrawer(property);

            itemGUIDValueASerializedProperty = property.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_A_PROPERTY_PATH);
            itemGUIDValueBSerializedProperty = property.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_B_PROPERTY_PATH);
            itemLastKnowNameSerializedProperty = property.FindPropertyRelative(COLLECTION_ITEM_LAST_KNOW_NAME_PROPERTY_PATH);
            collectionGUIDValueASerializedProperty = property.FindPropertyRelative(COLLECTION_GUID_VALUE_A_PROPERTY_PATH);
            collectionGUIDValueBSerializedProperty = property.FindPropertyRelative(COLLECTION_GUID_VALUE_B_PROPERTY_PATH);
            collectionLastKnowNameSerializedProperty = property.FindPropertyRelative(COLLECTION_LAST_KNOW_NAME_PROPERTY_PATH);

            TryGetCollectionItem(out ScriptableObject collectionItem);
            
            int indexOfArrayPart = property.propertyPath.IndexOf('[');
            if (indexOfArrayPart > -1)
            {
                if (string.Equals(label.text, itemGUIDValueASerializedProperty.longValue.ToString(), StringComparison.Ordinal))
                {
                    label.text = $"Element {property.propertyPath.Substring(indexOfArrayPart + 1, 1)}";
                }
            }

            if (collectionItemPropertyDrawer.OptionsAttribute.DrawType == DrawType.Dropdown)
            {
                collectionItemPropertyDrawer.DrawCollectionItemDrawer(ref position, property, collectionItem, label, item =>
                {
                    var element = property.serializedObject.FindProperty(property.propertyPath);
                    SetSerializedPropertyGUIDs(element, item);
                    property.serializedObject.ApplyModifiedProperties();
                });
                return;
            }

            EditorGUI.PropertyField(position, property, label, true);
        }

        private void SetSerializedPropertyGUIDs(SerializedProperty element, ScriptableObject item)
        {
            SerializedProperty itemA = element.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_A_PROPERTY_PATH);
            SerializedProperty itemB = element.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_B_PROPERTY_PATH);
            SerializedProperty itemName = element.FindPropertyRelative(COLLECTION_ITEM_LAST_KNOW_NAME_PROPERTY_PATH);
            SerializedProperty colA = element.FindPropertyRelative(COLLECTION_GUID_VALUE_A_PROPERTY_PATH);
            SerializedProperty colB = element.FindPropertyRelative(COLLECTION_GUID_VALUE_B_PROPERTY_PATH);
            SerializedProperty colName = element.FindPropertyRelative(COLLECTION_LAST_KNOW_NAME_PROPERTY_PATH);

            if (item == null)
            {
                itemA.longValue = 0;
                itemB.longValue = 0;
                colA.longValue = 0;
                colB.longValue = 0;
                itemName.stringValue = string.Empty;
                colName.stringValue = string.Empty;
                return;
            }

            if (item is ISOCItem socItem)
            {
                (long ia, long ib) = socItem.GUID.GetRawValues();
                itemA.longValue = ia;
                itemB.longValue = ib;
                itemName.stringValue = socItem.name;

                (long ca, long cb) = socItem.Collection.GUID.GetRawValues();
                colA.longValue = ca;
                colB.longValue = cb;
                colName.stringValue = socItem.Collection.name;
            }

        }

        private bool TryGetCollectionItem(out ScriptableObject item)
        {
            item = null;

            if (itemGUIDValueASerializedProperty.longValue == 0 || itemGUIDValueBSerializedProperty.longValue == 0 ||
                collectionGUIDValueASerializedProperty.longValue == 0 || collectionGUIDValueBSerializedProperty.longValue == 0)
            {
                return false;
            }
            
            LongGuid itemGUID = new LongGuid(itemGUIDValueASerializedProperty.longValue, itemGUIDValueBSerializedProperty.longValue);
            LongGuid collectionGUID = new LongGuid(collectionGUIDValueASerializedProperty.longValue, collectionGUIDValueBSerializedProperty.longValue);
            
            if (!CollectionsRegistry.Instance.TryGetCollectionByGUID(collectionGUID, out ScriptableObjectCollection collection))
                return false;
            
            if (!collection.TryGetItemByGUID(itemGUID, out ScriptableObject resultItem))
                return false;

            item = resultItem;
            return true;
        }

        private void CreateCollectionItemPropertyDrawer(SerializedProperty serializedProperty)
        {
            collectionItemPropertyDrawer = new CollectionItemPropertyDrawer();
            collectionItemPropertyDrawer.Initialize(collectionItemType, serializedProperty, GetOptionsAttribute());
        }

        private void SetCollectionItemType()
        {
            Type arrayOrListType = fieldInfo.FieldType.GetArrayOrListType();
            Type properFieldType = arrayOrListType ?? fieldInfo.FieldType;
            collectionItemType = properFieldType.GetBaseGenericType().GetGenericArguments()[0];
        }

        private SOCItemEditorOptionsAttribute GetOptionsAttribute()
        {
            if (fieldInfo == null)
                return null;
            object[] attributes = fieldInfo.GetCustomAttributes(typeof(SOCItemEditorOptionsAttribute), false);
            if (attributes.Length > 0)
                return attributes[0] as SOCItemEditorOptionsAttribute;

            return null;
        }
    }
}
