using System;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
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
                ScriptableObjectCollectionItem collectable = collection.AddNew(collection.GetItemType());
                callback?.Invoke(collectable);
                Selection.objects = new Object[] {collection};
                CollectionCustomEditor.LastAddedEnum = collectable;
                return;
            }
            
            if (item is CollectionItemDropdownItem collectableDropdownItem)
                callback?.Invoke(collectableDropdownItem.CollectionItem);
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
