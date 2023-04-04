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
    public class CollectionItemPicker<TItemType> : IList<TItemType>
        where TItemType : ScriptableObject, ISOCItem
    {
        [SerializeField] 
        private List<TItemType> items = new List<TItemType>();
        
        [SerializeField]
        private List<LongGuid> itemsGuids = new List<LongGuid>();


        private ScriptableObjectCollection cachedCollection;
        private ScriptableObjectCollection Collection
        {
            get
            {
                if (cachedCollection == null)
                {
                    if (CollectionsRegistry.Instance.TryGetCollectionFromItemType(typeof(TItemType),
                            out ScriptableObjectCollection result))
                        cachedCollection = result;
                }

                return cachedCollection;
            }
        }
        
        public event Action<TItemType> onItemTypeAddedEvent;
        public event Action<TItemType> onItemTypeRemovedEvent;
        public event Action onChangedEvent;

        #region Boleans and Checks
        public bool HasAny(params TItemType[] itemTypes)
        {
            for (int i = 0; i < itemTypes.Length; i++)
            {
                if (Contains(itemTypes[i]))
                    return true;
            }

            return false;
        }
        
        public bool HasAll(params TItemType[] itemTypes)
        {
            for (int i = 0; i < itemTypes.Length; i++)
            {
                if (!Contains(itemTypes[i]))
                    return false;
            }

            return true;
        }
        
        public bool HasNone(params TItemType[] itemTypes)
        {
            for (int i = 0; i < itemTypes.Length; i++)
            {
                if (Contains(itemTypes[i]))
                    return false;
            }

            return true;
        }
        #endregion
        
        //Implement mathematical operators  
        #region Operators

        public static CollectionItemPicker<TItemType> operator +(CollectionItemPicker<TItemType> picker1,
            CollectionItemPicker<TItemType> picker2)
        {
            CollectionItemPicker<TItemType> result = new CollectionItemPicker<TItemType>();

            for (int i = 0; i < picker1.Count; i++)
            {
                result.Add(picker1[i]);
            }

            for (int i = 0; i < picker2.Count; i++)
            {
                TItemType item = picker2[i];
                if (result.Contains(item))
                    continue;

                result.Add(item);
            }

            return result;
        }

        public static CollectionItemPicker<TItemType> operator -(CollectionItemPicker<TItemType> picker1,
            CollectionItemPicker<TItemType> picker2)
        {
            CollectionItemPicker<TItemType> result = new CollectionItemPicker<TItemType>();

            for (int i = 0; i < picker1.Count; i++)
            {
                result.Add(picker1[i]);
            }

            for (int i = 0; i < picker2.Count; i++)
            {
                TItemType item = picker2[i];
                if (!result.Contains(item))
                    continue;

                result.Remove(item);
            }

            return result;
        }

        public static CollectionItemPicker<TItemType> operator +(CollectionItemPicker<TItemType> picker,
            TItemType targetItem)
        {
            if (!picker.Contains(targetItem))
            {
                picker.Add(targetItem);
            }

            return picker;
        }

        public static CollectionItemPicker<TItemType> operator -(CollectionItemPicker<TItemType> picker,
            TItemType targetItem)
        {
            picker.Remove(targetItem);
            return picker;
        }

        #endregion

        // Implement IList and forward its members to items. This way we can conveniently use this thing as a list.
        #region IList members implementation

        public IEnumerator<TItemType> GetEnumerator()
        {
            return (IEnumerator<TItemType>) Collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) itemsGuids).GetEnumerator();
        }

        public void Add(TItemType item)
        {
            itemsGuids.Add(item.GUID);
            onItemTypeAddedEvent?.Invoke(item);
            onChangedEvent?.Invoke();
        }

        public void Clear()
        {
            itemsGuids.Clear();
            onChangedEvent?.Invoke();
        }

        public bool Contains(TItemType item)
        {
            for (int i = 0; i < itemsGuids.Count; i++)
            {
                if (itemsGuids[i] == item.GUID)
                    return true;
            }

            return false;
        }

        public void CopyTo(TItemType[] array, int arrayIndex)
        {
            for (int i = 0; i < itemsGuids.Count; i++)
            {
                if (!Collection.TryGetItemByGUID(itemsGuids[i], out ScriptableObject item))
                    continue;

                array[arrayIndex + i] = (TItemType) item;
            }
        }

        public bool Remove(TItemType item)
        {
            TItemType removedItem = item;
            bool removed = itemsGuids.Remove(item.GUID);
            if (removed)
            {
                onChangedEvent?.Invoke();
                onItemTypeRemovedEvent?.Invoke(removedItem);
            }

            return removed;
        }

        public int Count => itemsGuids.Count;

        public bool IsReadOnly => false;

        public int IndexOf(TItemType item)
        {
            return itemsGuids.IndexOf(item.GUID);
        }

        public void Insert(int index, TItemType item)
        {
            itemsGuids.Insert(index, item.GUID);
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= itemsGuids.Count)
                return;

            if (!Collection.TryGetItemByGUID(itemsGuids[index], out var item))
                return;

            TItemType removedItem = (TItemType) item;
            itemsGuids.RemoveAt(index);
            onChangedEvent?.Invoke();
            onItemTypeRemovedEvent?.Invoke(removedItem);
        }

        public TItemType this[int index]
        {
            get
            {
                if (!Collection.TryGetItemByGUID(itemsGuids[index], out var item))
                    return null;
                return item as TItemType;
            }
            set => itemsGuids[index] = value.GUID;
        }

        #endregion

    }
}
