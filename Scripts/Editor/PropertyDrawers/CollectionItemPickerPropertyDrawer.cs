﻿using System;
using System.Collections.Generic;
using System.Linq;
using BrunoMikoski.ScriptableObjectCollections.Popup;
using UnityEditor;
using UnityEngine;
using PopupWindow = UnityEditor.PopupWindow;

namespace BrunoMikoski.ScriptableObjectCollections.Picker
{
    [CustomPropertyDrawer(typeof(CollectionItemPicker<>), true)]
    public class CollectionItemPickerPropertyDrawer : PropertyDrawer
    {
        private const string COLLECTION_ITEM_GUID_VALUE_A = "collectionItemGUIDValueA";
        private const string COLLECTION_ITEM_GUID_VALUE_B = "collectionItemGUIDValueB";

        private const string COLLECTION_GUID_VALUE_A = "collectionGUIDValueA";
        private const string COLLECTION_GUID_VALUE_B = "collectionGUIDValueB";
        
        private const string ITEMS_PROPERTY_NAME = "indirectReferences";

        private static GUIStyle labelStyle;
        private static GUIStyle buttonStyle;
        private float buttonHeight = EditorGUIUtility.singleLineHeight;
        private List<ScriptableObjectCollection> possibleCollections;
        private List<ScriptableObject> availableItems = new();

        private readonly HashSet<string> initializedPropertiesPaths = new();

        private readonly Dictionary<string, PopupList<PopupItem>> propertyPathToPopupList = new();

        private readonly struct PopupItem : IPopupListItem
        {
            private readonly string name;
            public string Name => name;
            
            public PopupItem(ScriptableObject scriptableObject)
            {
                name = scriptableObject.name;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return Mathf.Max(buttonHeight,
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Initialize(property);

            PopupList<PopupItem> popupList = propertyPathToPopupList[property.propertyPath];

            position = EditorGUI.PrefixLabel(position, label);

            Rect totalPosition = position;
            Rect buttonRect = position;

            totalPosition.height = buttonHeight;
            buttonRect.height = buttonHeight;

            float buttonWidth = 20f;
            Rect plusButtonRect = new Rect(position.xMax - buttonWidth, position.y, buttonWidth, buttonHeight);

            totalPosition.width -= buttonWidth;

            if (!popupList.IsOpen)
            {
                SetSelectedValuesOnPopup(popupList, property);
            }

            if (GUI.Button(totalPosition, "", buttonStyle))
            {
                EditorWindow inspectorWindow = EditorWindow.focusedWindow;

                popupList.OnClosedEvent += () =>
                {
                    GetValuesFromPopup(popupList, property);
                    SetSelectedValuesOnPopup(popupList, property);
                };
                popupList.OnItemSelectedEvent += (x, y) => { inspectorWindow.Repaint(); };
                PopupWindow.Show(buttonRect, popupList);
            }

            using (new EditorGUI.DisabledScope(possibleCollections.Count > 1))
            {
                if (GUI.Button(plusButtonRect, "+"))
                {
                    CreatAndAddNewItems(property);
                }
            }

            buttonRect.width = 0;

            Rect labelRect = buttonRect;

            labelRect.y += 2;
            labelRect.height -= 4;

            float currentLineWidth = position.x + 4;
            float maxHeight = 0;
            float inspectorWidth = EditorGUIUtility.currentViewWidth - 88;
            float currentLineMaxHeight = 0;

            Color originalColor = GUI.backgroundColor;
            for (int i = 0; i < popupList.Count; i++)
            {
                if (!popupList.GetSelected(i))
                    continue;

                ScriptableObject collectionItem = availableItems[i];
                GUIContent labelContent = new GUIContent(collectionItem.name);
                Vector2 size = labelStyle.CalcSize(labelContent);

                if (currentLineWidth + size.x + 4 > inspectorWidth)
                {
                    labelRect.y += currentLineMaxHeight + 4;
                    maxHeight += currentLineMaxHeight + 4;
                    currentLineWidth = position.x + 4;
                    currentLineMaxHeight = 0;
                }

                currentLineMaxHeight = Mathf.Max(currentLineMaxHeight, size.y);

                labelRect.x = currentLineWidth;
                labelRect.width = size.x;

                currentLineWidth += size.x + 4;

                if (collectionItem is ISOCColorizedItem coloredItem)
                    GUI.backgroundColor = coloredItem.LabelColor;
                else
                    GUI.backgroundColor = Color.black;

                GUI.Label(labelRect, labelContent, labelStyle);
            }

            GUI.backgroundColor = originalColor;

            maxHeight += currentLineMaxHeight;

            buttonHeight = Mathf.Max(maxHeight + EditorGUIUtility.standardVerticalSpacing * 3,
                EditorGUIUtility.singleLineHeight);

            EditorGUI.EndProperty();
        }

        private void CreatAndAddNewItems(SerializedProperty property)
        {
            ScriptableObjectCollection scriptableObjectCollection = possibleCollections.First();
            ScriptableObjectCollection collection = scriptableObjectCollection;

            ScriptableObject newItem = CollectionCustomEditor.AddNewItem(collection, scriptableObjectCollection.GetItemType());
            SerializedProperty itemsProperty = property.FindPropertyRelative(ITEMS_PROPERTY_NAME);
            itemsProperty.arraySize++;

            AssignItemGUIDToProperty(newItem, itemsProperty.GetArrayElementAtIndex(itemsProperty.arraySize - 1));
        }

        private void GetValuesFromPopup(PopupList<PopupItem> popupList, SerializedProperty property)
        {
            SerializedProperty itemsProperty = property.FindPropertyRelative(ITEMS_PROPERTY_NAME);
            itemsProperty.ClearArray();

            int selectedCount = 0;
            for (int i = 0; i < popupList.Count; i++)
            {
                if (popupList.GetSelected(i))
                {
                    selectedCount++;
                }
            }

            itemsProperty.arraySize = selectedCount;

            int propertyArrayIndex = 0;

            for (int i = 0; i < popupList.Count; i++)
            {
                if (popupList.GetSelected(i))
                {
                    SerializedProperty newProperty = itemsProperty.GetArrayElementAtIndex(propertyArrayIndex);
                    AssignItemGUIDToProperty(availableItems[i], newProperty);
                    propertyArrayIndex++;
                }
            }

            itemsProperty.serializedObject.ApplyModifiedProperties();
        }

        private void AssignItemGUIDToProperty(ScriptableObject scriptableObject, SerializedProperty newProperty)
        {
            if (scriptableObject is not ISOCItem item)
                return;

            (long, long) itemValues = item.GUID.GetRawValues();
            (long, long) collectionValues = item.Collection.GUID.GetRawValues();

            newProperty.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_A).longValue = itemValues.Item1;
            newProperty.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_B).longValue = itemValues.Item2;

            newProperty.FindPropertyRelative(COLLECTION_GUID_VALUE_A).longValue = collectionValues.Item1;
            newProperty.FindPropertyRelative(COLLECTION_GUID_VALUE_B).longValue = collectionValues.Item2;
        }

        private void SetSelectedValuesOnPopup(PopupList<PopupItem> popupList, SerializedProperty property)
        {
            popupList.DeselectAll();

            SerializedProperty itemsProperty = property.FindPropertyRelative(ITEMS_PROPERTY_NAME);

            int arraySize = itemsProperty.arraySize;
            for (int i = arraySize - 1; i >= 0; i--)
            {
                SerializedProperty elementProperty = itemsProperty.GetArrayElementAtIndex(i);

                LongGuid socItemGUID = GetGUIDFromProperty(elementProperty);

                ScriptableObject foundItem = availableItems.FirstOrDefault(so =>
                {
                    if (so is ISOCItem item)
                    {
                        if(item.GUID == socItemGUID)
                        {
                            return true;
                        }
                    }

                    return false;
                });
                if (foundItem != null)
                {
                    int indexOf = availableItems.IndexOf(foundItem);
                    if (indexOf >= 0)
                    {
                        popupList.SetSelected(indexOf, true);
                    }
                    else
                    {
                        itemsProperty.DeleteArrayElementAtIndex(i);
                    }
                }
                else
                {
                    itemsProperty.DeleteArrayElementAtIndex(i);
                }
            }

            itemsProperty.serializedObject.ApplyModifiedProperties();
        }

        private LongGuid GetGUIDFromProperty(SerializedProperty property)
        {
            SerializedProperty itemGUIDValueASerializedProperty = property.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_A);
            SerializedProperty itemGUIDValueBSerializedProperty = property.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_B);

            return new LongGuid(itemGUIDValueASerializedProperty.longValue, itemGUIDValueBSerializedProperty.longValue);
        }

        private void Initialize(SerializedProperty property)
        {
            if (initializedPropertiesPaths.Contains(property.propertyPath))
                return;

            ValidateIndirectReferencesInProperty(property);

            Type arrayOrListType = fieldInfo.FieldType.GetArrayOrListType();
            Type itemType = arrayOrListType ?? fieldInfo.FieldType;

            if (itemType.IsGenericType)
                itemType = itemType.GetGenericArguments()[0];

            if (!CollectionsRegistry.Instance.TryGetCollectionsOfItemType(itemType, out possibleCollections))
                throw new Exception($"No collection found for item type {itemType}");

            propertyPathToPopupList.Add(property.propertyPath, new PopupList<PopupItem>());

            availableItems.Clear();
            for (int i = 0; i < possibleCollections.Count; i++)
            {
                for (int j = 0; j < possibleCollections[i].Count; j++)
                {
                    ScriptableObject scriptableObject = possibleCollections[i][j];
                    Type scriptableObjectType = scriptableObject.GetType();

                    if (scriptableObjectType != itemType && !scriptableObjectType.IsSubclassOf(itemType))
                        continue;

                    availableItems.Add(scriptableObject);
                    propertyPathToPopupList[property.propertyPath].AddItem(new PopupItem(scriptableObject), false);
                }
            }

            buttonStyle = EditorStyles.textArea;
            GUIStyle assetLabelStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("AssetLabel"));
            labelStyle = assetLabelStyle;
            initializedPropertiesPaths.Add(property.propertyPath);
        }

        private void ValidateIndirectReferencesInProperty(SerializedProperty property)
        {
            SerializedProperty indirectReferencesProperty = property.FindPropertyRelative(ITEMS_PROPERTY_NAME);

            bool changed = false;
            for (int i = indirectReferencesProperty.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty elementProperty = indirectReferencesProperty.GetArrayElementAtIndex(i);

                long collectionGUIDValueA = elementProperty.FindPropertyRelative(COLLECTION_GUID_VALUE_A).longValue;
                long collectionGUIDValueB = elementProperty.FindPropertyRelative(COLLECTION_GUID_VALUE_B).longValue;
                LongGuid collectionGUID = new(collectionGUIDValueA, collectionGUIDValueB);

                long itemGUIDValueA = elementProperty.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_A).longValue;
                long itemGUIDValueB = elementProperty.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_B).longValue;
                LongGuid itemGUID = new(itemGUIDValueA, itemGUIDValueB);

                bool validReference = false;
                if(CollectionsRegistry.Instance.TryGetCollectionByGUID(collectionGUID, out ScriptableObjectCollection collection))
                {
                    if (collection.TryGetItemByGUID(itemGUID, out _))
                    {
                        validReference = true;
                    }
                }

                if (!validReference)
                {
                    indirectReferencesProperty.DeleteArrayElementAtIndex(i);
                    changed = true;
                }
            }

            if (changed)
            {
                indirectReferencesProperty.serializedObject.ApplyModifiedProperties();
            }
        }


    }
}