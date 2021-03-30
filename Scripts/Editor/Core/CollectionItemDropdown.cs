using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public sealed class CollectionItemDropdown : AdvancedDropdown
    {
        private const string CREATE_NEW_TEXT = "+ Create New";
        private readonly ScriptableObjectCollection collection;
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
            root.AddSeparator();
            for (int i = 0; i < collection.Count; i++)
            {
                ScriptableObjectCollectionItem collectionItem = collection[i];
                root.AddChild(new CollectionItemDropdownItem(collectionItem));
            }
            root.AddSeparator();
            root.AddChild(new AdvancedDropdownItem(CREATE_NEW_TEXT));
            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);

            if (item.name.Equals(CREATE_NEW_TEXT, StringComparison.OrdinalIgnoreCase))
            {
                ScriptableObjectCollectionItem collectionItem = collection.AddNew(collection.GetItemType());
                callback?.Invoke(collectionItem);
                Selection.objects = new Object[] {collection};
                CollectionCustomEditor.SetLastAddedEnum(collectionItem);
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
