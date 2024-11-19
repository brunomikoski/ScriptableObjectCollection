using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BrunoMikoski.ScriptableObjectCollections.Picker
{
    /// <summary>
    /// Collection Item Picker lets you pick one or more items from a collection, similar to how an enum field would
    /// work if the enum had the [Flags] attribute applied to it.
    /// </summary>
    [Serializable]
    public class CollectionItemPicker<TItemType> : IList<TItemType>, IEquatable<IList<TItemType>>, IEquatable<CollectionItemPicker<TItemType>>
        where TItemType : ScriptableObject, ISOCItem
    {
        [SerializeField, FormerlySerializedAs("cachedIndirectReferences")]
        private List<CollectionItemIndirectReference<TItemType>> indirectReferences = new();

        public event Action<TItemType> OnItemTypeAddedEvent;
        public event Action<TItemType> OnItemTypeRemovedEvent;
        public event Action OnChangedEvent;

        private bool isDirty = true;
        private List<TItemType> cachedItems = new();
        public List<TItemType> Items
        {
            get
            {
                if (!Application.isPlaying || isDirty)
                {
                    cachedItems.Clear();

                    for (int i = 0; i < indirectReferences.Count; i++)
                    {
                        CollectionItemIndirectReference<TItemType> collectionItemIndirectReference = indirectReferences[i];
                        if (!collectionItemIndirectReference.IsValid() || collectionItemIndirectReference.Ref == null)
                        {
                            indirectReferences.RemoveAt(i);
                            continue;
                        }

                        cachedItems.Add(collectionItemIndirectReference.Ref);
                    }

                    isDirty = false;
                }

                return cachedItems;
            }
        }

        public CollectionItemPicker()
        {
            
        }
        
        public CollectionItemPicker(params TItemType[] items)
        {
            for (int i = 0; i < items.Length; i++)
                Add(items[i]);
        }

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
        
        public bool HasAll(IList<TItemType> itemTypes)
        {
            for (int i = 0; i < itemTypes.Count; i++)
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

        public static implicit operator List<TItemType>(CollectionItemPicker<TItemType> targetPicker)
        {
            return targetPicker.Items;
        }

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
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        public void Add(TItemType item)
        {
            if (Contains(item))
                return;
            
            indirectReferences.Add(new CollectionItemIndirectReference<TItemType>(item));
            isDirty = true;
            OnItemTypeAddedEvent?.Invoke(item);
            OnChangedEvent?.Invoke();
        }

        public void Clear()
        {
            indirectReferences.Clear();
            isDirty = true;
            OnChangedEvent?.Invoke();
        }

        public bool Contains(TItemType item)
        {
            for (int i = 0; i < indirectReferences.Count; i++)
            {
                CollectionItemIndirectReference<TItemType> indirectReference = indirectReferences[i];
                if (indirectReference.Ref == item)
                    return true;
            }

            return false;
        }

        public void CopyTo(TItemType[] array, int arrayIndex)
        {
            for (int i = 0; i < indirectReferences.Count; i++)
            {
                CollectionItemIndirectReference<TItemType> indirectReference = indirectReferences[i];
                
                array[arrayIndex + i] = indirectReference.Ref;

            }
        }

        private bool TryFindIndirectReferenceByItemType(TItemType targetItemType, out CollectionItemIndirectReference<TItemType> result)
        {
            for (int i = 0; i < indirectReferences.Count; i++)
            {
                CollectionItemIndirectReference<TItemType> indirectReference = indirectReferences[i];
                if (indirectReference.Ref == targetItemType)
                {
                    result = indirectReference;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public bool Remove(TItemType item)
        {
            if (!TryFindIndirectReferenceByItemType(item, out CollectionItemIndirectReference<TItemType> indirectReference))
                return false;
            
            CollectionItemIndirectReference<TItemType> removedItem = indirectReference;
            bool removed = indirectReferences.Remove(indirectReference);
            if (removed)
            {
                isDirty = true;
                OnChangedEvent?.Invoke();
                OnItemTypeRemovedEvent?.Invoke(removedItem.Ref);
            }

            return removed;
        }

        public int Count => indirectReferences.Count;

        public bool IsReadOnly => false;

        public int IndexOf(TItemType item)
        {
            return indirectReferences.FindIndex(reference => reference.Ref.GUID == item.GUID);
        }

        public void Insert(int index, TItemType item)
        {
            if (Contains(item))
                return;
            
            indirectReferences.Insert(index, new CollectionItemIndirectReference<TItemType>(item));
            isDirty = true;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= indirectReferences.Count)
                return;

            CollectionItemIndirectReference<TItemType> removedItem = indirectReferences[index];
            indirectReferences.RemoveAt(index);
            isDirty = true;
            OnChangedEvent?.Invoke();
            OnItemTypeRemovedEvent?.Invoke(removedItem.Ref);
        }

        public TItemType this[int index]
        {
            get => indirectReferences[index].Ref;
            set
            {
                indirectReferences[index] = new CollectionItemIndirectReference<TItemType>(value);
                isDirty = true;
            }
        }

        #endregion

        public bool Equals(IList<TItemType> other)
        {
            if (other == null)
                return false;

            if (other.Count != Count)
                return false;

            for (int i = 0; i < other.Count; i++)
            {
                if (!Contains(other[i]))
                    return false;
            }

            return true;
        }

        public bool Equals(CollectionItemPicker<TItemType> other)
        {
            if (other == null)
                return false;

            if (other.Count != Count)
                return false;

            for (int i = 0; i < other.Count; i++)
            {
                if (!Contains(other[i]))
                    return false;
            }

            return true;
        }
    }
}