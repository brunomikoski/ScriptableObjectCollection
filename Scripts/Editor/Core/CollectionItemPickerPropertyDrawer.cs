using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Lets you pick one or more items from a collection, similar to how an enum field would work if the enum had the
    /// [Flags] attribute applied to it.
    /// </summary>
    [CustomPropertyDrawer(typeof(CollectionItemPicker<>))]
    public class CollectionItemPickerPropertyDrawer : PropertyDrawer
    {
        private readonly List<ScriptableObject>
            tempMaskItems = new List<ScriptableObject>();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        private static bool Contains(SerializedProperty itemsProperty, ScriptableObject item)
        {
            for (int i = 0; i < itemsProperty.arraySize; i++)
            {
                if (itemsProperty.GetArrayElementAtIndex(i).objectReferenceValue == item)
                    return true;
            }

            return false;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Figure out the collection item type.
            Type[] genericArguments = fieldInfo.FieldType.GetGenericArguments();
            Type collectionItemType = genericArguments[0];

            // Now figure out the collection type.
            ScriptableObjectCollection collection;
            CollectionsRegistry.Instance.TryGetCollectionFromItemType(collectionItemType, out collection);
            
            // TODO: Should this support multiple collections? I'm not sure how that use case works. I just saw it
            // being used elsewhere.

            if (collection == null)
            {
                EditorGUI.LabelField(position, label, new GUIContent("Could not determine collection."));
                return;
            }
            
            // Get the list of items.
            SerializedProperty itemsProperty = property.FindPropertyRelative("items");
            
            // Reserve space for the buttons. We can't use AdvancedDropdown for Flags kind of behaviour, so we have
            // to use a real MaskField to get it done. I still want to support adding a new entry from the inspector
            // though. For that reason I am adding an Add button next to the dropdown.
            position.width -= SOCItemPropertyDrawer.BUTTON_WIDTH * 2;
            Rect goToButtonRect = new Rect(
                position.xMax, position.y, SOCItemPropertyDrawer.BUTTON_WIDTH, position.height);
            Rect addButtonRect = new Rect(
                goToButtonRect.xMax, position.y, SOCItemPropertyDrawer.BUTTON_WIDTH, position.height);
            
            // If the collection is empty, we cannot use MaskField with an empty array because that throws exceptions.
            // Because of that we treat it as a special case where we draw a disabled PopUp field. You can then use
            // the Go To and Add buttons to add an item to the collection and then you can begin picking.
            if (collection.Count == 0)
            {
                // Calculate the rects for the label and the add button and such.
                Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
                Rect valueRect = new Rect(
                    position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth,
                    position.height);

                // Draw the label.
                EditorGUI.LabelField(labelRect, label);
                
                // Draw the inactive dropdown.
                bool wasGuiEnabled = GUI.enabled;
                GUI.enabled = false;
                EditorGUI.Popup(valueRect, GUIContent.none, 0, new[] {new GUIContent("")});
                GUI.enabled = wasGuiEnabled;
            }
            else
            {
                // Create an array of displayed options.
                string[] displayedOptions = new string[collection.Count];
                int mask = 0;
                for (int i = 0; i < collection.Count; i++)
                {
                    displayedOptions[i] = collection[i].name;
                    if (Contains(itemsProperty, collection[i]))
                        mask |= 1 << i;
                }

                int maskNew = EditorGUI.MaskField(position, label, mask, displayedOptions);
                if (mask != maskNew)
                {
                    // First convert the newly selected mask to a list of items.
                    tempMaskItems.Clear();
                    for (int i = 0; i < collection.Count; i++)
                    {
                        int flag = 1 << i;
                        if ((maskNew & flag) == flag)
                            tempMaskItems.Add(collection[i]);
                    }

                    // Now update the property to have the values in that list...
                    itemsProperty.arraySize = tempMaskItems.Count;
                    for (int i = 0; i < tempMaskItems.Count; i++)
                    {
                        itemsProperty.GetArrayElementAtIndex(i).objectReferenceValue = tempMaskItems[i];
                    }
                }
            }
            
            // Draw the Go To button.
            bool shouldGoToCollection = GUI.Button(goToButtonRect, CollectionEditorGUI.ARROW_RIGHT_CHAR);
            if (shouldGoToCollection)
                Selection.activeObject = collection;
            
            // Draw the add button.
            bool shouldCreateNewItem = GUI.Button(addButtonRect, "+");
            if (shouldCreateNewItem)
                CollectionCustomEditor.AddNewItem(collection, collectionItemType);
        }
    }
}
