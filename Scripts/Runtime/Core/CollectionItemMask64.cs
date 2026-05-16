using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class CollectionItemMask64
    {
        public const int BitCount = 64;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidIndex(int index)
        {
            return index >= 0 && index < BitCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Bit(int index)
        {
            return 1UL << index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has(ulong mask, int index)
        {
            return (mask & Bit(index)) != 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Add(ulong mask, int index)
        {
            return mask | Bit(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Remove(ulong mask, int index)
        {
            return mask & ~Bit(index);
        }

        // Set-comparison helpers. Caller is responsible for ensuring both masks were built
        // from items of the same collection — bit positions are per-collection.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Overlaps(ulong a, ulong b)
        {
            return (a & b) != 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSubsetOf(ulong subset, ulong superset)
        {
            return (subset & ~superset) == 0UL;
        }

        public static ulong From<T>(IEnumerable<T> items, out bool fits)
            where T : ScriptableObject, ISOCItem
        {
            fits = true;
            ulong mask = 0UL;
            if (items == null)
                return 0UL;

            foreach (T item in items)
            {
                if (!item)
                    continue;

                ScriptableObjectCollectionItem socItem = item as ScriptableObjectCollectionItem;
                if (socItem == null)
                    continue;

                int index = socItem.Index;
                if (!IsValidIndex(index))
                {
                    fits = false;
                    continue;
                }

                mask |= Bit(index);
            }

            return mask;
        }
    }

    public static class CollectionItemMask64Extensions
    {
        public static ulong ToItemMask64<T>(this IEnumerable<T> items)
            where T : ScriptableObject, ISOCItem
        {
            return CollectionItemMask64.From(items, out _);
        }

        public static ulong ToItemMask64<T>(this IEnumerable<T> items, out bool fits)
            where T : ScriptableObject, ISOCItem
        {
            return CollectionItemMask64.From(items, out fits);
        }

        // Fills `list` with the items in `collection` whose Index is set in `mask`. Clears the list first.
        // Bits set beyond collection.Count or beyond CollectionItemMask64.BitCount are silently ignored.
        public static void FromItemMask64<T>(this List<T> list, ulong mask, ScriptableObjectCollection collection)
            where T : ScriptableObject, ISOCItem
        {
            if (list == null || collection == null)
                return;

            list.Clear();
            List<ScriptableObject> items = collection.Items;
            int count = Math.Min(items.Count, CollectionItemMask64.BitCount);
            for (int i = 0; i < count; i++)
            {
                if (CollectionItemMask64.Has(mask, i))
                    list.Add((T)items[i]);
            }
        }
    }
}
