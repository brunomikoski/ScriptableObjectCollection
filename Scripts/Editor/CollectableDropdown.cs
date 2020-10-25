using System;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public sealed class CollectableDropdown : AdvancedDropdown
    {
        private ScriptableObjectCollection collection;
        private Action<CollectableScriptableObject> callback;

        public CollectableDropdown(AdvancedDropdownState state, ScriptableObjectCollection collection) : base(state)
        {
            this.collection = collection;
            this.minimumSize = new Vector2(200, 300);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            Type collectableType = collection.GetCollectionType();
            AdvancedDropdownItem root = new AdvancedDropdownItem(collectableType.Name);

            root.AddChild(new AdvancedDropdownItem("None"));
            for (int i = 0; i < collection.Items.Count; i++)
            {
                CollectableScriptableObject collectionItem = collection.Items[i];
                if (collectionItem.GetType() == collectableType)
                    root.AddChild(new CollectableDropdownItem(collectionItem));
                else
                {
                    AdvancedDropdownItem parent = GetOrCreateDropdownItemForType(root, collectionItem);
                    parent.AddChild(new CollectableDropdownItem(collectionItem));
                }
            }
            return root;
        }

        private AdvancedDropdownItem GetOrCreateDropdownItemForType(AdvancedDropdownItem root,
            CollectableScriptableObject collectionItem)
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
            if (item is CollectableDropdownItem collectableDropdownItem)
                callback?.Invoke(collectableDropdownItem.Collectable);
            else
                callback?.Invoke(null);
        }

        public void Show(Rect rect, Action<CollectableScriptableObject> onSelectedCallback)
        {
            callback = onSelectedCallback;
            base.Show(rect);
        }
    }
}
