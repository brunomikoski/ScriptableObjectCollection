using System;
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
        
        private const string ITEMS_PROPERTY_NAME = "cachedIndirectReferences";

        private bool initialized;
        private PopupList<PopupItem> popupList = new PopupList<PopupItem>();
        private static GUIStyle labelStyle;
        private static GUIStyle buttonStyle;
        private float buttonHeight = EditorGUIUtility.singleLineHeight;
        private List<ScriptableObjectCollection> possibleCollections;
        private List<ScriptableObject> availableItems = new List<ScriptableObject>();

        private readonly struct PopupItem : IPopupListItem
        {
            private readonly string name;
            public string Name => name;
            
            private readonly LongGuid collectionGUID;
            public LongGuid CollectionGuid => collectionGUID;

            private readonly LongGuid socItemGUID;
            public LongGuid SocItemGuid => socItemGUID;


            public PopupItem(ScriptableObject scriptableObject)
            {
                name = scriptableObject.name;
                if (scriptableObject is ISOCItem socItem)
                {
                    collectionGUID = socItem.Collection.GUID;
                    socItemGUID = socItem.GUID;
                    return;
                }

                collectionGUID = default;
                socItemGUID = default;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return Mathf.Max(buttonHeight,
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Initialize(property);

            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, label);

            Rect totalPosition = position;
            Rect buttonRect = position;

            totalPosition.height = buttonHeight;
            buttonRect.height = buttonHeight;

            if (!popupList.IsOpen)
                SetSelectedValuesOnPopup(property);

            if (GUI.Button(totalPosition, "", buttonStyle))
            {
                EditorWindow inspectorWindow = EditorWindow.focusedWindow;

                popupList.OnClosedEvent += () =>
                {
                    GetValuesFromPopup(property);
                    SetSelectedValuesOnPopup(property);
                };
                popupList.OnItemSelectedEvent += (x, y) => { inspectorWindow.Repaint(); };
                PopupWindow.Show(buttonRect, popupList);
            }

            buttonRect.width = 0;

            Rect labelRect = buttonRect;

            labelRect.y += 2;
            labelRect.height -= 4;

            float currentLineWidth = position.x + 4;
            float maxHeight = 0;
            float inspectorWidth = EditorGUIUtility.currentViewWidth;
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

        private void GetValuesFromPopup(SerializedProperty property)
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
            if (scriptableObject is ISOCItem item)
            {
                (long, long) itemValues = item.GUID.GetRawValues();
                (long, long) collectionValues = item.Collection.GUID.GetRawValues();

                newProperty.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_A).longValue = itemValues.Item1;
                newProperty.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_B).longValue = itemValues.Item2;

                newProperty.FindPropertyRelative(COLLECTION_GUID_VALUE_A).longValue = collectionValues.Item1;
                newProperty.FindPropertyRelative(COLLECTION_GUID_VALUE_B).longValue = collectionValues.Item2;
            }
        }

        private void SetSelectedValuesOnPopup(SerializedProperty property)
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
        }

        private LongGuid GetGUIDFromProperty(SerializedProperty property)
        {
            SerializedProperty itemGUIDValueASerializedProperty = property.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_A);
            SerializedProperty itemGUIDValueBSerializedProperty = property.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_B);

            return new LongGuid(itemGUIDValueASerializedProperty.longValue, itemGUIDValueBSerializedProperty.longValue);
        }


        private void Initialize(SerializedProperty property)
        {
            if (initialized)
                return;
            
            Type arrayOrListType = fieldInfo.FieldType.GetArrayOrListType();
            Type itemType = arrayOrListType ?? fieldInfo.FieldType;

            if (itemType.IsGenericType)
                itemType = itemType.GetGenericArguments()[0];

            if (!CollectionsRegistry.Instance.TryGetCollectionsOfItemType(itemType, out possibleCollections))
                throw new Exception($"No collection found for item type {itemType}");

            popupList.Clear();
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
                    popupList.AddItem(new PopupItem(scriptableObject), false);
                }
            }

            buttonStyle = EditorStyles.textArea;
            GUIStyle assetLabelStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("AssetLabel"));
            labelStyle = assetLabelStyle;
            initialized = true;
        }
    }
}
