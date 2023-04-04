using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private readonly Object owner;
        private readonly MethodInfo validationMethod;

        public CollectionItemDropdown(AdvancedDropdownState state, Type targetItemType,
            SOCItemEditorOptionsAttribute options, Object owner) : base(state)
        {
            itemType = targetItemType;
            collections = CollectionsRegistry.Instance.GetCollectionsByItemType(itemType);
            minimumSize = new Vector2(200, 300);
            this.options = options;
            this.owner = owner;


            if (options != null && !string.IsNullOrEmpty(options.ValidateMethod))
            {
                validationMethod = owner.GetType().GetMethod(options.ValidateMethod, new[] {itemType});
            }
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem root = new AdvancedDropdownItem(itemType.Name);

            root.AddChild(new AdvancedDropdownItem("None"));
            root.AddSeparator();

            AdvancedDropdownItem targetParent = root;
            bool multipleCollections = collections.Count > 1;
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection collection = collections[i];

                if (multipleCollections)
                {
                    AdvancedDropdownItem collectionParent = new AdvancedDropdownItem(collection.name);
                    root.AddChild(collectionParent);
                    targetParent = collectionParent;
                }

                for (int j = 0; j < collection.Count; j++)
                {
                    ScriptableObject collectionItem = collection[j];
                    
                    if (validationMethod != null)
                    {
                        bool result = (bool) validationMethod.Invoke(owner, new object[] {collectionItem});
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

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);

            if (item.name.Equals(CREATE_NEW_TEXT, StringComparison.OrdinalIgnoreCase))
            {
                ScriptableObjectCollection collection = collections.First();
                ScriptableObject newItem = CollectionCustomEditor.AddNewItem(collection, itemType);
                callback.Invoke(newItem);
                return;
            }
            
            if (item is CollectionItemDropdownItem dropdownItem)
                callback.Invoke(dropdownItem.CollectionItem);
            else
                callback.Invoke(null);
        }

        public void Show(Rect rect, Action<ScriptableObject> onSelectedCallback)
        {
            callback = onSelectedCallback;
            base.Show(rect);
        }
    }
}
