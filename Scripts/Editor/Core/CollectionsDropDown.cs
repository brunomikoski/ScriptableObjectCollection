using System;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

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
            root.AddSeparator();
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
}
