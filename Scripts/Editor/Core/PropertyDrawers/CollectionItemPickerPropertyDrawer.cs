using System;
using BrunoMikoski.ScriptableObjectCollections.Popup;
using UnityEditor;
using UnityEngine;
using PopupWindow = UnityEditor.PopupWindow;

namespace BrunoMikoski.ScriptableObjectCollections.Picker
{
    [CustomPropertyDrawer(typeof(CollectionItemPicker<>), true)]
    public class CollectionItemPickerPropertyDrawer : PropertyDrawer
    {
        private const string LONG_GUID_VALUE_1_PROPERTY_PATH = "value1";
        private const string LONG_GUID_VALUE_2_PROPERTY_PATH = "value2";

        private const string ITEMS_PROPERTY_NAME = "itemsGuids";

        private bool initialized;
        private PopupList<PopupItem> popupList = new PopupList<PopupItem>();
        private static GUIStyle labelStyle;
        private static GUIStyle buttonStyle;
        private ScriptableObjectCollection collection;
        private float buttonHeight = EditorGUIUtility.singleLineHeight;

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
                ScriptableObject item = collection[i];

                if (!popupList.GetSelected(i))
                    continue;

                GUIContent labelContent = new GUIContent(item.name);
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

                if (item is ISOCColorizedItem coloredItem)
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
                    AssignItemGUIDToProperty(collection[i], newProperty);
                    propertyArrayIndex++;
                }
            }

            itemsProperty.serializedObject.ApplyModifiedProperties();
        }

        private void AssignItemGUIDToProperty(ScriptableObject scriptableObject, SerializedProperty newProperty)
        {
            SerializedProperty itemGUIDValueASerializedProperty = newProperty.FindPropertyRelative(LONG_GUID_VALUE_1_PROPERTY_PATH);
            SerializedProperty itemGUIDValueBSerializedProperty = newProperty.FindPropertyRelative(LONG_GUID_VALUE_2_PROPERTY_PATH);

            if (scriptableObject is ISOCItem item)
            {
                (long, long) values = item.GUID.GetRawValues();
                itemGUIDValueASerializedProperty.longValue = values.Item1;
                itemGUIDValueBSerializedProperty.longValue = values.Item2;
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


                if (collection.TryGetItemByGUID(socItemGUID, out ScriptableObject result))
                {
                    int indexOf = collection.IndexOf(result);
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
            SerializedProperty itemGUIDValueASerializedProperty = property.FindPropertyRelative(LONG_GUID_VALUE_1_PROPERTY_PATH);
            SerializedProperty itemGUIDValueBSerializedProperty = property.FindPropertyRelative(LONG_GUID_VALUE_2_PROPERTY_PATH);

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
            
            if (!CollectionsRegistry.Instance.TryGetCollectionFromItemType(itemType, out ScriptableObjectCollection collection))
                throw new Exception($"No collection found for item type {itemType}");


            popupList.Clear();
            for (int i = 0; i < collection.Count; i++)
            {
                ScriptableObject scriptableObject = collection[i];
                popupList.AddItem(new PopupItem(scriptableObject), false);
            }

            this.collection = collection;

            buttonStyle = EditorStyles.textArea;
            GUIStyle assetLabelStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("AssetLabel"));
            labelStyle = assetLabelStyle;
            initialized = true;
        }
    }
}
