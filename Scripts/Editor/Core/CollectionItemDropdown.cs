using System;
using System.Collections.Generic;
using System.Linq;
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

            AdvancedDropdownItem targetParent = root;
            bool multipleCollections = collections.Count > 1;
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection collection = collections[i];
                
                // If we're meant to constrain the selection to a specific collection, enforce that now.
                if (shouldConstrainToCollection && collectionToConstrainTo != collection)
                    continue;

                // If there are multiple collections, group them together.
                if (multipleCollections && !shouldConstrainToCollection)
                {
                    AdvancedDropdownItem collectionParent = new AdvancedDropdownItem(collection.name);
                    root.AddChild(collectionParent);
                    targetParent = collectionParent;
                }

                // Add every individual item in the collection.
                for (int j = 0; j < collection.Count; j++)
                {
                    ScriptableObject collectionItem = collection[j];

                    if (!itemType.IsInstanceOfType(collectionItem))
                        continue;
                    
                    if (validationMethod != null)
                    {
                        bool result = (bool) validationMethod.Invoke(
                            serializedProperty.serializedObject.targetObject, new object[] {collectionItem});
                        if (!result)
                            continue;
                    }

                    targetParent.AddChild(new CollectionItemDropdownItem(collectionItem));
                }
            }

            if (!multipleCollections && !itemType.IsAbstract)
            {
                root.AddSeparator();
                root.AddChild(new AdvancedDropdownItem(CREATE_NEW_TEXT));
            }
            return root;
        }

        private void InvokeOnSelectCallback(ScriptableObject from, ScriptableObject to)
        {
            if (onSelectCallbackMethod == null)
                return;

            object target = onSelectCallbackMethod.IsStatic ? null : serializedProperty.serializedObject.targetObject;
            onSelectCallbackMethod.Invoke(target, new object[] {from, to});
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);

            ScriptableObject previousValue = null;
            if (onSelectCallbackMethod != null)
                previousValue = serializedProperty.objectReferenceValue as ScriptableObject;

            if (item.name.Equals(CREATE_NEW_TEXT, StringComparison.OrdinalIgnoreCase))
            {
                ScriptableObjectCollection collection = collections.First();
                ScriptableObject newItem = CollectionCustomEditor.AddNewItem(collection, itemType);
                callback.Invoke(newItem);
                
                InvokeOnSelectCallback(previousValue, newItem);
                
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
