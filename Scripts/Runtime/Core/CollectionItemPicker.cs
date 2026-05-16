using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

namespace BrunoMikoski.ScriptableObjectCollections.Picker
{
    /// <summary>
    /// Collection Item Picker lets you pick one or more items from a collection, similar to how an enum field would
    /// work if the enum had the [Flags] attribute applied to it.
    /// </summary>
    [Serializable]
    public class CollectionItemPicker<TItemType> : IList<TItemType>, IEquatable<IList<TItemType>>, IEquatable<CollectionItemPicker<TItemType>>, ISerializationCallbackReceiver
        where TItemType : ScriptableObject, ISOCItem
    {
        [SerializeField, FormerlySerializedAs("cachedIndirectReferences")]
        private List<CollectionItemIndirectReference<TItemType>> indirectReferences = new();

        public event Action<TItemType> OnItemAddedEvent;
        public event Action<TItemType> OnItemRemovedEvent;
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

                    for (int i = indirectReferences.Count - 1; i >= 0; i--)
                    {
                        CollectionItemIndirectReference<TItemType> collectionItemIndirectReference = indirectReferences[i];
                        if (!collectionItemIndirectReference.IsValid() || collectionItemIndirectReference.Ref == null)
                        {
                            indirectReferences.RemoveAt(i);
                            continue;
                        }

                        cachedItems.Add(collectionItemIndirectReference.Ref);
                    }

                    cachedItems.Reverse();

                    isDirty = false;
                }

                return cachedItems;
            }
        }

        [NonSerialized] private ulong cachedMask;
        [NonSerialized] private bool isMaskDirty = true;
        [NonSerialized] private bool canUseBitmask;

        // True when (a) every item's Index fits in 64 bits AND (b) every item's collection
        // has SupportsBitmaskIndexing == true. Callers should gate any bitmask fast-path on
        // this flag and fall back to GUID-based comparison otherwise.
        public bool CanUseBitmask { get { EnsureMaskCache(); return canUseBitmask; } }
        public ulong CachedMask   { get { EnsureMaskCache(); return cachedMask;   } }

        private void EnsureMaskCache()
        {
            if (Application.isPlaying && !isMaskDirty)
                return;

            cachedMask = CollectionItemMask64.From(Items, out bool fits);
            canUseBitmask = fits && AllItemCollectionsAllowBitmask();
            isMaskDirty = false;
        }

        private bool AllItemCollectionsAllowBitmask()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                return Items[i].Collection.SupportsBitmaskIndexing;
            }
            return true;
        }

        public int CountMatchesIn(ulong targetMask)
        {
            EnsureMaskCache();
            return PopCount(targetMask & cachedMask);
        }

        public int CountMatchesIn(IEnumerable<TItemType> targetItems)
        {
            if (targetItems == null)
                return 0;

            if (CanUseBitmask)
            {
                ulong targetMask = CollectionItemMask64.From(targetItems, out _);
                return PopCount(targetMask & cachedMask);
            }

            int n = 0;
            foreach (TItemType item in targetItems)
            {
                if (item != null && Contains(item))
                    n++;
            }
            return n;
        }

        // Counts the number of set bits in a 64-bit value (a.k.a. "population count" / popcount).
        // Standard SWAR algorithm: sums bits in pairs, then nibbles, then bytes, in parallel
        // across the whole word. Each line halves the number of subgroups being summed:
        //   line 1: pairs of bits  -> 32 x 2-bit counts (each 0..2)
        //   line 2: nibbles        -> 16 x 4-bit counts (each 0..4)
        //   line 3: bytes          ->  8 x 8-bit counts (each 0..8)
        //   line 4: multiply by 0x01010101_01010101 to sum the 8 byte counts into the high byte,
        //          then shift down 56 bits to read it.
        // Used in place of System.Numerics.BitOperations.PopCount, which isn't available in
        // this Unity build's API surface.
        private static int PopCount(ulong x)
        {
            x = x - ((x >> 1) & 0x5555555555555555UL);
            x = (x & 0x3333333333333333UL) + ((x >> 2) & 0x3333333333333333UL);
            x = (x + (x >> 4)) & 0x0F0F0F0F0F0F0F0FUL;
            return (int)((x * 0x0101010101010101UL) >> 56);
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
            isMaskDirty = true;
            OnItemAddedEvent?.Invoke(item);
            OnChangedEvent?.Invoke();
        }

        public void Clear()
        {
            indirectReferences.Clear();
            isDirty = true;
            isMaskDirty = true;
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
                isMaskDirty = true;
                OnChangedEvent?.Invoke();
                OnItemRemovedEvent?.Invoke(removedItem.Ref);
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
            isMaskDirty = true;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= indirectReferences.Count)
                return;

            CollectionItemIndirectReference<TItemType> removedItem = indirectReferences[index];
            indirectReferences.RemoveAt(index);
            isDirty = true;
            isMaskDirty = true;
            OnChangedEvent?.Invoke();
            OnItemRemovedEvent?.Invoke(removedItem.Ref);
        }

        public TItemType this[int index]
        {
            get => indirectReferences[index].Ref;
            set
            {
                indirectReferences[index] = new CollectionItemIndirectReference<TItemType>(value);
                isDirty = true;
                isMaskDirty = true;
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

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            isDirty = true;
            isMaskDirty = true;
        }

        public override string ToString()
        {
            if (Items.Count == 0)
                return "[]";

            StringBuilder builder = new StringBuilder();
            builder.Append('[');
            for (int i = 0; i < Items.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");

                TItemType item = Items[i];
                builder.Append(item != null ? item.name : "<null>");
            }
            builder.Append(']');
            return builder.ToString();
        }
    }
}
