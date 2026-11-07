using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public sealed class CollectionItemDropdown : AdvancedDropdown
    {
        private const string CREATE_NEW_TEXT = "+ Create New";
        private Action<ScriptableObject> callback;
        private readonly List<ScriptableObjectCollection> collections;

        private readonly Type itemType;
        private readonly SOCItemEditorOptionsAttribute options;
        private readonly SerializedProperty serializedProperty;
        private readonly MethodInfo validationMethod;
        
        private readonly MethodInfo onSelectCallbackMethod;

        public CollectionItemDropdown(AdvancedDropdownState state, Type targetItemType,
            SOCItemEditorOptionsAttribute options, SerializedProperty serializedProperty) : base(state)
        {
            itemType = targetItemType;
            collections = CollectionsRegistry.Instance.GetCollectionsByItemType(itemType);
            minimumSize = new Vector2(200, 300);
            this.options = options;
            this.serializedProperty = serializedProperty;

            if (options != null)
            {
                Object owner = serializedProperty.serializedObject.targetObject;
                if (!string.IsNullOrEmpty(options.ValidateMethod))
                    validationMethod = owner.GetType().GetMethod(options.ValidateMethod, new[] {itemType});
                
                // If it's specified that a callback should be fired when an item is selected, cache that callback.
                if (!string.IsNullOrEmpty(options.OnSelectCallbackMethod))
                {
                    onSelectCallbackMethod = owner.GetType().GetMethod(options.OnSelectCallbackMethod,
                        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        null, new[] {itemType, itemType}, null);
                    if (onSelectCallbackMethod == null)
                    {
                        Debug.LogWarning($"Component '{owner.name}' wants selection callback " +
                                         $"'{options.OnSelectCallbackMethod}' which is not a valid method.");
                    }
                }
            }
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem root = new AdvancedDropdownItem(itemType.Name);

            root.AddChild(new AdvancedDropdownItem("None"));
            root.AddSeparator();

            // If specified, limit the displayed items to those of a collection specified in a certain field.
            ScriptableObjectCollection collectionToConstrainTo = null;
            if (!string.IsNullOrEmpty(options.ConstrainToCollectionField))
            {
                SerializedProperty collectionField = serializedProperty.serializedObject.FindProperty(
                    options.ConstrainToCollectionField);
                if (collectionField == null)
                {
                    Debug.LogWarning($"Tried to constrain dropdown to collection specified in field " +
                                     $"'{options.ConstrainToCollectionField}' but no such field existed in " +
                                     $"'{serializedProperty.serializedObject.targetObject}'");
                    return root;
                }

                collectionToConstrainTo = collectionField.objectReferenceValue as ScriptableObjectCollection;
                if (collectionToConstrainTo == null)
                {
                    Debug.LogWarning($"Tried to constrain dropdown to collection specified in field " +
                                     $"'{options.ConstrainToCollectionField}' but no collection was specified.");
                    return root;
                }
            }
            bool shouldConstrainToCollection = collectionToConstrainTo != null;

            List<ScriptableObjectCollection> visibleCollections = new List<ScriptableObjectCollection>();
            List<List<ScriptableObject>> visibleItems = new List<List<ScriptableObject>>();
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection collection = collections[i];

                // If we're meant to constrain the selection to a specific collection, enforce that now.
                if (shouldConstrainToCollection && collectionToConstrainTo != collection)
                    continue;

                // A collection of a base type is also returned here, but it may hold no item of the target type.
                List<ScriptableObject> selectableItems = GetSelectableItems(collection);
                if (selectableItems.Count == 0)
                    continue;

                visibleCollections.Add(collection);
                visibleItems.Add(selectableItems);
            }

            bool groupByCollection = visibleCollections.Count > 1;
            for (int i = 0; i < visibleCollections.Count; i++)
            {
                AdvancedDropdownItem targetParent = root;
                if (groupByCollection)
                {
                    targetParent = new AdvancedDropdownItem(visibleCollections[i].name);
                    root.AddChild(targetParent);
                }

                List<ScriptableObject> selectableItems = visibleItems[i];
                for (int j = 0; j < selectableItems.Count; j++)
                    targetParent.AddChild(new CollectionItemDropdownItem(selectableItems[j]));
            }

            AddCreateNewOption(root, collectionToConstrainTo);
            return root;
        }

        private void AddCreateNewOption(AdvancedDropdownItem root, ScriptableObjectCollection collectionToConstrainTo)
        {
            if (itemType.IsAbstract)
                return;

            if (!TryGetCreateNewCollection(out ScriptableObjectCollection createNewCollection))
                return;

            if (collectionToConstrainTo != null && collectionToConstrainTo != createNewCollection)
                return;

            root.AddSeparator();
            root.AddChild(new AdvancedDropdownItem(CREATE_NEW_TEXT));
        }

        private bool TryGetCreateNewCollection(out ScriptableObjectCollection result)
        {
            result = null;

            Type mostSpecificItemType = null;
            for (int i = 0; i < collections.Count; i++)
            {
                Type collectionItemType = collections[i].GetItemType();
                if (!collectionItemType.IsAssignableFrom(itemType))
                    continue;

                if (mostSpecificItemType != null && !mostSpecificItemType.IsAssignableFrom(collectionItemType))
                    continue;

                mostSpecificItemType = collectionItemType;
            }

            if (mostSpecificItemType == null)
                return false;

            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection collection = collections[i];
                if (collection.GetItemType() != mostSpecificItemType)
                    continue;

                if (result != null)
                {
                    result = null;
                    return false;
                }

                result = collection;
            }

            return true;
        }

        private List<ScriptableObject> GetSelectableItems(ScriptableObjectCollection collection)
        {
            List<ScriptableObject> result = new List<ScriptableObject>();
            for (int i = 0; i < collection.Count; i++)
            {
                ScriptableObject collectionItem = collection[i];

                if (!itemType.IsInstanceOfType(collectionItem))
                    continue;

                if (validationMethod != null)
                {
                    bool isValid = (bool) validationMethod.Invoke(
                        serializedProperty.serializedObject.targetObject, new object[] {collectionItem});
                    if (!isValid)
                        continue;
                }

                result.Add(collectionItem);
            }

            return result;
        }

        private void InvokeOnSelectCallback(ScriptableObject from, ScriptableObject to)
        {
            if (onSelectCallbackMethod == null)
                return;

            object[] arguments = { from, to };
            
            // The method may be static, in which case there is no target.
            if (onSelectCallbackMethod.IsStatic)
            {
                onSelectCallbackMethod.Invoke(null, arguments);
                return;
            }

            // Otherwise, fire the callback on every target.
            for (int i = 0; i < serializedProperty.serializedObject.targetObjects.Length; i++)
            {
                object target = serializedProperty.serializedObject.targetObjects[i];
                onSelectCallbackMethod.Invoke(target, arguments);
            }
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);

            ScriptableObject previousValue = null;
            if (onSelectCallbackMethod != null)
                previousValue = serializedProperty.objectReferenceValue as ScriptableObject;

            if (item.name.Equals(CREATE_NEW_TEXT, StringComparison.OrdinalIgnoreCase))
            {
                if (!TryGetCreateNewCollection(out ScriptableObjectCollection collection))
                    return;

                TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom(itemType);
                if (types.Count == 0)
                {
                    ScriptableObject newItem = CollectionCustomEditor.AddNewItem(collection, itemType);
                    callback.Invoke(newItem);
                    InvokeOnSelectCallback(previousValue, newItem);
                }
                else
                {
                    Selection.activeObject = collection;
                }
                
                return;
            }
            
            if (item is CollectionItemDropdownItem dropdownItem)
            {
                callback.Invoke(dropdownItem.CollectionItem);
                InvokeOnSelectCallback(previousValue, dropdownItem.CollectionItem);
            }
            else
            {
                callback.Invoke(null);
                InvokeOnSelectCallback(previousValue, null);
            }
        }

        public void Show(Rect rect, Action<ScriptableObject> onSelectedCallback)
        {
            callback = onSelectedCallback;
            base.Show(rect);
        }
    }
}
