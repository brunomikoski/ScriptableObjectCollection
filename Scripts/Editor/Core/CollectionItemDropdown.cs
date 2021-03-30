using System;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public sealed class CollectionsDropDown : AdvancedDropdown
    {
        private ScriptableObjectCollection[] availableCollections;
        private Action<ScriptableObjectCollection> callback;

        public CollectionsDropDown(AdvancedDropdownState state, ScriptableObjectCollection[] scriptableObjectCollections) : base(state)
        {
            availableCollections = scriptableObjectCollections;
            this.minimumSize = new Vector2(200, 300);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem root =
                new AdvancedDropdownItem($"{availableCollections.First().GetItemType().Name} Collections");
            
            root.AddChild(new CollectionDropdownItem("None"));

            for (int i = 0; i < availableCollections.Length; i++)
            {
                root.AddChild(new CollectionDropdownItem(availableCollections[i]));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);
            if (item is CollectionDropdownItem collectionDropdownItem)
            {
                callback?.Invoke(collectionDropdownItem.collection);
            }
        }

        public void Show(Rect rect, Action<ScriptableObjectCollection> onSelectedCallback)
        {
            callback = onSelectedCallback;
            base.Show(rect);
        }
    }
    public sealed class CollectionItemDropdown : AdvancedDropdown
    {
        private const string CREATE_NEW_TEXT = "+ Create New";
        private ScriptableObjectCollection collection;
        private Action<ScriptableObjectCollectionItem> callback;

        public CollectionItemDropdown(AdvancedDropdownState state, ScriptableObjectCollection collection) : base(state)
        {
            this.collection = collection;
            this.minimumSize = new Vector2(200, 300);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            Type collectionItemType = collection.GetItemType();
            AdvancedDropdownItem root = new AdvancedDropdownItem(collectionItemType.Name);

            root.AddChild(new AdvancedDropdownItem("None"));
            for (int i = 0; i < collection.Count; i++)
            {
                ScriptableObjectCollectionItem collectionItem = collection[i];
                if (collectionItem.GetType() == collectionItemType)
                    root.AddChild(new CollectionItemDropdownItem(collectionItem));
                else
                {
                    AdvancedDropdownItem parent = GetOrCreateDropdownItemForType(root, collectionItem);
                    parent.AddChild(new CollectionItemDropdownItem(collectionItem));
                }
            }

            root.AddChild(new AdvancedDropdownItem(CREATE_NEW_TEXT));
            return root;
        }

        private AdvancedDropdownItem GetOrCreateDropdownItemForType(AdvancedDropdownItem root,
            ScriptableObjectCollectionItem collectionItem)
        {
            AdvancedDropdownItem item = root.children.FirstOrDefault(dropdownItem =>
                dropdownItem.name.Equals(collectionItem.GetType().Name, StringComparison.Ordinal));
            if (item == null)
            {
                item = new AdvancedDropdownItem(collectionItem.GetType().Name);
                root.AddChild(item);
            }

            return item;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);

            if (item.name.Equals(CREATE_NEW_TEXT, StringComparison.OrdinalIgnoreCase))
            {
                ScriptableObjectCollectionItem collectionItem = collection.AddNew(collection.GetItemType());
                callback?.Invoke(collectionItem);
                Selection.objects = new Object[] {collection};
                CollectionCustomEditor.LastAddedEnum = collectionItem;
                return;
            }
            
            if (item is CollectionItemDropdownItem dropdownItem)
                callback?.Invoke(dropdownItem.CollectionItem);
            else
                callback?.Invoke(null);
        }

        public void Show(Rect rect, Action<ScriptableObjectCollectionItem> onSelectedCallback)
        {
            callback = onSelectedCallback;
            base.Show(rect);
        }
    }
}
