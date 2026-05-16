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
            private readonly ScriptableObject scriptableObject;
            public string Name => name;
            public ScriptableObject ScriptableObject => scriptableObject;

            public PopupItem(ScriptableObject scriptableObject)
            {
                this.scriptableObject = scriptableObject;
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

            GatherItemOccurrences(property, out int targetCount, out int[] occurrences);
            bool isMultiTarget = targetCount > 1;

            if (GUI.Button(totalPosition, "", buttonStyle))
            {
                EditorWindow inspectorWindow = EditorWindow.focusedWindow;

                popupList.OnItemSelectedEvent += (popupItem, isSelected) =>
                {
                    ApplyItemChangeToTargets(popupItem, isSelected, property);
                    if (inspectorWindow != null)
                        inspectorWindow.Repaint();
                };
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
            Color originalGuiColor = GUI.color;
            for (int i = 0; i < popupList.Count; i++)
            {
                if (!popupList.GetSelected(i))
                    continue;

                ScriptableObject collectionItem = availableItems[i];

                bool isMixed = isMultiTarget
                    && i < occurrences.Length
                    && occurrences[i] > 0
                    && occurrences[i] < targetCount;

                GUIContent labelContent = new GUIContent(
                    collectionItem.name,
                    isMixed ? "Value differs across selected objects" : null);
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

                if (isMixed)
                    GUI.color = new Color(originalGuiColor.r, originalGuiColor.g, originalGuiColor.b, originalGuiColor.a * 0.45f);
                else
                    GUI.color = originalGuiColor;

                GUI.Label(labelRect, labelContent, labelStyle);
            }

            GUI.backgroundColor = originalColor;
            GUI.color = originalGuiColor;

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

            string propertyPath = property.propertyPath;
            UnityEngine.Object[] targets = property.serializedObject.targetObjects;

            foreach (UnityEngine.Object target in targets)
            {
                SerializedObject targetSerializedObject = new SerializedObject(target);
                SerializedProperty targetProperty = targetSerializedObject.FindProperty(propertyPath);
                if (targetProperty == null)
                    continue;

                SerializedProperty itemsProperty = targetProperty.FindPropertyRelative(ITEMS_PROPERTY_NAME);
                itemsProperty.arraySize++;
                AssignItemGUIDToProperty(newItem, itemsProperty.GetArrayElementAtIndex(itemsProperty.arraySize - 1));
                targetSerializedObject.ApplyModifiedProperties();
            }
        }

        private void ApplyItemChangeToTargets(PopupItem popupItem, bool selected, SerializedProperty property)
        {
            ScriptableObject scriptableObject = popupItem.ScriptableObject;
            if (scriptableObject == null || scriptableObject is not ISOCItem socItem)
                return;

            LongGuid itemGUID = socItem.GUID;
            string propertyPath = property.propertyPath;
            UnityEngine.Object[] targets = property.serializedObject.targetObjects;

            foreach (UnityEngine.Object target in targets)
            {
                SerializedObject targetSerializedObject = new SerializedObject(target);
                SerializedProperty targetProperty = targetSerializedObject.FindProperty(propertyPath);
                if (targetProperty == null)
                    continue;

                SerializedProperty itemsProperty = targetProperty.FindPropertyRelative(ITEMS_PROPERTY_NAME);

                int existingIndex = -1;
                for (int i = 0; i < itemsProperty.arraySize; i++)
                {
                    SerializedProperty element = itemsProperty.GetArrayElementAtIndex(i);
                    if (GetGUIDFromProperty(element) == itemGUID)
                    {
                        existingIndex = i;
                        break;
                    }
                }

                if (selected && existingIndex < 0)
                {
                    itemsProperty.arraySize++;
                    SerializedProperty newElement = itemsProperty.GetArrayElementAtIndex(itemsProperty.arraySize - 1);
                    AssignItemGUIDToProperty(scriptableObject, newElement);
                    targetSerializedObject.ApplyModifiedProperties();
                }
                else if (!selected && existingIndex >= 0)
                {
                    itemsProperty.DeleteArrayElementAtIndex(existingIndex);
                    targetSerializedObject.ApplyModifiedProperties();
                }
            }
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

            newProperty.FindPropertyRelative("itemLastKnownName").stringValue = item.name;
            newProperty.FindPropertyRelative("collectionLastKnownName").stringValue = item.Collection.name;
        }

        private void SetSelectedValuesOnPopup(PopupList<PopupItem> popupList, SerializedProperty property)
        {
            popupList.DeselectAll();

            Dictionary<LongGuid, int> guidToIndex = new();
            for (int i = 0; i < availableItems.Count; i++)
            {
                if (availableItems[i] is ISOCItem item)
                    guidToIndex[item.GUID] = i;
            }

            string propertyPath = property.propertyPath;
            UnityEngine.Object[] targets = property.serializedObject.targetObjects;

            HashSet<int> aggregatedIndices = new();

            foreach (UnityEngine.Object target in targets)
            {
                SerializedObject targetSerializedObject = new SerializedObject(target);
                SerializedProperty targetProperty = targetSerializedObject.FindProperty(propertyPath);
                if (targetProperty == null)
                    continue;

                SerializedProperty itemsProperty = targetProperty.FindPropertyRelative(ITEMS_PROPERTY_NAME);

                bool changed = false;
                for (int i = itemsProperty.arraySize - 1; i >= 0; i--)
                {
                    SerializedProperty elementProperty = itemsProperty.GetArrayElementAtIndex(i);
                    LongGuid socItemGUID = GetGUIDFromProperty(elementProperty);

                    if (guidToIndex.TryGetValue(socItemGUID, out int indexOf))
                    {
                        aggregatedIndices.Add(indexOf);
                    }
                    else
                    {
                        itemsProperty.DeleteArrayElementAtIndex(i);
                        changed = true;
                    }
                }

                if (changed)
                    targetSerializedObject.ApplyModifiedProperties();
            }

            foreach (int index in aggregatedIndices)
                popupList.SetSelected(index, true);
        }

        private void GatherItemOccurrences(SerializedProperty property, out int targetCount, out int[] occurrences)
        {
            string propertyPath = property.propertyPath;
            UnityEngine.Object[] targets = property.serializedObject.targetObjects;
            targetCount = targets.Length;
            occurrences = new int[availableItems.Count];

            Dictionary<LongGuid, int> guidToIndex = new();
            for (int i = 0; i < availableItems.Count; i++)
            {
                if (availableItems[i] is ISOCItem item)
                    guidToIndex[item.GUID] = i;
            }

            foreach (UnityEngine.Object target in targets)
            {
                SerializedObject targetSerializedObject = new SerializedObject(target);
                SerializedProperty targetProperty = targetSerializedObject.FindProperty(propertyPath);
                if (targetProperty == null)
                    continue;

                SerializedProperty itemsProperty = targetProperty.FindPropertyRelative(ITEMS_PROPERTY_NAME);

                HashSet<int> seenThisTarget = new();
                for (int i = 0; i < itemsProperty.arraySize; i++)
                {
                    SerializedProperty elementProperty = itemsProperty.GetArrayElementAtIndex(i);
                    LongGuid socItemGUID = GetGUIDFromProperty(elementProperty);

                    if (guidToIndex.TryGetValue(socItemGUID, out int indexOf) && seenThisTarget.Add(indexOf))
                        occurrences[indexOf]++;
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
            string propertyPath = property.propertyPath;
            UnityEngine.Object[] targets = property.serializedObject.targetObjects;

            foreach (UnityEngine.Object target in targets)
            {
                SerializedObject targetSerializedObject = new SerializedObject(target);
                SerializedProperty targetProperty = targetSerializedObject.FindProperty(propertyPath);
                if (targetProperty == null)
                    continue;

                SerializedProperty indirectReferencesProperty = targetProperty.FindPropertyRelative(ITEMS_PROPERTY_NAME);

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
                    if (CollectionsRegistry.Instance.TryGetCollectionByGUID(collectionGUID, out ScriptableObjectCollection collection))
                    {
                        if (collection.TryGetItemByGUID(itemGUID, out _))
                        {
                            validReference = true;
                        }
                    }

                    if (!validReference)
                    {
                        SerializedProperty lastKnownItemNameProperty = elementProperty.FindPropertyRelative("itemLastKnownName");
                        SerializedProperty lastKnownCollectionNameProperty = elementProperty.FindPropertyRelative("collectionLastKnownName");

                        string lastKnownItemName = lastKnownItemNameProperty != null ? lastKnownItemNameProperty.stringValue : string.Empty;
                        string lastKnownCollectionName = lastKnownCollectionNameProperty != null ? lastKnownCollectionNameProperty.stringValue : string.Empty;

                        if (!string.IsNullOrEmpty(lastKnownItemName) || !string.IsNullOrEmpty(lastKnownCollectionName))
                        {
                            string ownerName = target != null ? target.name : "Unknown Owner";

                            Debug.LogError(
                                $"Missing collection item reference in CollectionItemPicker.\n" +
                                $"Item Tag: '{lastKnownItemName}'\n" +
                                $"Collection: '{lastKnownCollectionName}'\n" +
                                $"Owner: '{ownerName}'\n" +
                                $"Property Path: '{propertyPath}'",
                                target);
                        }

                        indirectReferencesProperty.DeleteArrayElementAtIndex(i);
                        changed = true;
                    }
                }

                if (changed)
                {
                    targetSerializedObject.ApplyModifiedProperties();
                }
            }
        }


    }
}