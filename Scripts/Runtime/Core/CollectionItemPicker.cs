using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Collection Item Picker lets you pick one or more items from a collection, similar to how an enum field would
    /// work if the enum had the [Flags] attribute applied to it.
    /// </summary>
    [Serializable]
    public class CollectionItemPicker<ItemType> : IList<ItemType>
        where ItemType : ScriptableObjectCollectionItem
    {
        [SerializeField] private List<ItemType> items = new List<ItemType>();
        public List<ItemType> Items => items;

        // Implement IList and forward its members to items. This way we can conveniently use this thing as a list.
        #region IList members implementation
        
        public IEnumerator<ItemType> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)items).GetEnumerator();
        }

        public void Add(ItemType item)
        {
            items.Add(item);
        }

        public void Clear()
        {
            items.Clear();
        }

        public bool Contains(ItemType item)
        {
            return items.Contains(item);
        }

        public void CopyTo(ItemType[] array, int arrayIndex)
        {
            items.CopyTo(array, arrayIndex);
        }

        public bool Remove(ItemType item)
        {
            return items.Remove(item);
        }

        public int Count => items.Count;

        public bool IsReadOnly => false;

        public int IndexOf(ItemType item)
        {
            return items.IndexOf(item);
        }

        public void Insert(int index, ItemType item)
        {
            items.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            items.RemoveAt(index);
        }

        public ItemType this[int index]
        {
            get => items[index];
            set => items[index] = value;
        }
        #endregion
    }
}
