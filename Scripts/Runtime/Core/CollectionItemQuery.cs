using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Picker
{
    [Serializable]
    public class CollectionItemQuery<T> where T : ScriptableObject, ISOCItem
    {
        public enum MatchType
        {
            /// <summary>Target has at least one of the picker items.</summary>
            [InspectorName("Has any of")]
            Any = CollectionItemQueryRule.Any,
            /// <summary>Target has every one of the picker items.</summary>
            [InspectorName("Has all of")]
            All = CollectionItemQueryRule.All,
            /// <summary>Target has none of the picker items (all picker items are absent).</summary>
            [InspectorName("Has none of")]
            NotAny = CollectionItemQueryRule.NotAny,
            /// <summary>Target is missing at least one of the picker items (not all are present).</summary>
            [InspectorName("Is missing one of")]
            NotAll = CollectionItemQueryRule.NotAll,
            /// <summary>Target has at least <see cref="QuerySet.MinimumCount"/> of the picker items.</summary>
            [InspectorName("Has at least")]
            AtLeast = CollectionItemQueryRule.AtLeast,
        }

        [Serializable]
        public class QuerySet
        {
            [SerializeField]
            private MatchType matchType;
            public MatchType MatchType => matchType;

            [SerializeField]
            private int minimumCount = 1;
            public int MinimumCount => minimumCount;

            [SerializeField]
            private CollectionItemPicker<T> picker;
            public CollectionItemPicker<T> Picker => picker;

            public override string ToString()
            {
                return picker != null ? picker.ToString() : "[]";
            }
        }

        [SerializeField]
        private QuerySet[] query = Array.Empty<QuerySet>();

        private HashSet<LongGuid> targetGuids = new HashSet<LongGuid>(128);
        private HashSet<LongGuid> pickerGuids = new HashSet<LongGuid>(64);

        public bool Matches(params T[] targetItems)
        {
            return Matches(targetItems, out _);
        }

        public bool Matches(IEnumerable<T> targetItems)
        {
            return Matches(targetItems, out _);
        }

        public bool Matches(IReadOnlyList<T> targetItems)
        {
            return Matches(targetItems, out _);
        }

        /// <summary>
        /// Fast path for picker targets (e.g. a tag list): when both this query's pickers and
        /// <paramref name="targetPicker"/> are bitmask-compatible on the same collection, matching
        /// is pure bit arithmetic on cached masks — no enumeration, no allocation. Falls back to
        /// <see cref="Matches(IEnumerable{T})"/> otherwise.
        /// </summary>
        public bool Matches(CollectionItemPicker<T> targetPicker)
        {
            return Matches(targetPicker, out _);
        }

        /// <inheritdoc cref="Matches(CollectionItemPicker{T})"/>
        public bool Matches(CollectionItemPicker<T> targetPicker, out int resultMatchCount)
        {
            resultMatchCount = 0;
            if (query.Length == 0)
                return true;

            if (targetPicker != null
                && targetPicker.CanUseBitmask
                && TryGetSharedBitmaskCollection(out ScriptableObjectCollection sharedCollection)
                && (sharedCollection == null
                    || targetPicker.MaskCollection == null
                    || targetPicker.MaskCollection == sharedCollection))
            {
                return MatchesViaBitmask(targetPicker.CachedMask, out resultMatchCount);
            }

            return Matches((IEnumerable<T>)targetPicker, out resultMatchCount);
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var querySet in query)
            {
                stringBuilder.Append(querySet.MatchType);
                stringBuilder.Append(" ");
                if (querySet.MatchType == MatchType.AtLeast)
                {
                    stringBuilder.Append(querySet.MinimumCount);
                    stringBuilder.Append(" ");
                }
                stringBuilder.Append(querySet);
                stringBuilder.Append(" ");
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Evaluates every <see cref="QuerySet"/> in the query against <paramref name="targetItems"/>.
        /// Returns <c>true</c> only if every set passes its <see cref="MatchType"/> check;
        /// an empty query returns <c>true</c>, and sets whose picker has no (resolvable) items are
        /// skipped as inert — a half-configured rule expresses no constraint.
        /// </summary>
        /// <param name="targetItems">The items to test against (e.g., the tags on a rigidbody). A null collection is treated as empty, so positive sets (<see cref="MatchType.Any"/>/<see cref="MatchType.All"/>/<see cref="MatchType.AtLeast"/>) fail and negative sets (<see cref="MatchType.NotAny"/>/<see cref="MatchType.NotAll"/>) pass. May be enumerated twice when the bitmask fast path detects a foreign-collection item and falls back to GUID matching.</param>
        /// <param name="resultMatchCount">Total number of individual picker items found across all query sets. Informational only (and partial when the method early-returns false); does not affect the return value.</param>
        public bool Matches(IEnumerable<T> targetItems, out int resultMatchCount)
        {
            resultMatchCount = 0;
            if (query.Length == 0)
                return true;

            if (TryGetSharedBitmaskCollection(out ScriptableObjectCollection sharedCollection))
            {
                ulong targetMask = CollectionItemMask64.From(targetItems, sharedCollection, out bool targetFits);
                if (targetFits)
                    return MatchesViaBitmask(targetMask, out resultMatchCount);
            }

            FillTargetGuids(targetItems);
            return MatchesViaGuids(out resultMatchCount);
        }

        public bool Matches(IReadOnlyList<T> targetItems, out int resultMatchCount)
        {
            resultMatchCount = 0;
            if (query.Length == 0)
                return true;

            if (TryGetSharedBitmaskCollection(out ScriptableObjectCollection sharedCollection))
            {
                ulong targetMask = CollectionItemMask64.From(targetItems, sharedCollection, out bool targetFits);
                if (targetFits)
                    return MatchesViaBitmask(targetMask, out resultMatchCount);
            }

            FillTargetGuids(targetItems);
            return MatchesViaGuids(out resultMatchCount);
        }

        // True when every picker can use the bitmask fast path AND all non-empty pickers agree on
        // one collection (bit positions are per-collection; comparing masks across collections
        // would let unrelated items collide). sharedCollection is null when every picker is empty.
        private bool TryGetSharedBitmaskCollection(out ScriptableObjectCollection sharedCollection)
        {
            sharedCollection = null;
            for (int i = 0; i < query.Length; i++)
            {
                CollectionItemPicker<T> picker = query[i].Picker;
                if (!picker.CanUseBitmask)
                    return false;

                ScriptableObjectCollection pickerCollection = picker.MaskCollection;
                if (pickerCollection == null)
                    continue;

                if (sharedCollection == null)
                    sharedCollection = pickerCollection;
                else if (pickerCollection != sharedCollection)
                    return false;
            }

            return true;
        }

        private bool MatchesViaBitmask(ulong targetMask, out int resultMatchCount)
        {
            resultMatchCount = 0;

            for (int i = 0; i < query.Length; i++)
            {
                QuerySet qs = query[i];

                int pickerCount = qs.Picker.MaskItemCount;
                if (pickerCount == 0)
                    continue;

                int matchCount = qs.Picker.CountMatchesIn(targetMask);
                resultMatchCount += matchCount;

                if (!CollectionItemQueryRule.Passes((int)qs.MatchType, matchCount, pickerCount, qs.MinimumCount))
                    return false;
            }

            return true;
        }

        private void FillTargetGuids(IEnumerable<T> targetItems)
        {
            targetGuids.Clear();
            if (targetItems == null)
                return;

            foreach (T item in targetItems)
            {
                if (item)
                    targetGuids.Add(item.GUID);
            }
        }

        private void FillTargetGuids(IReadOnlyList<T> targetItems)
        {
            targetGuids.Clear();
            if (targetItems == null)
                return;

            for (int i = 0; i < targetItems.Count; i++)
            {
                T item = targetItems[i];
                if (item)
                    targetGuids.Add(item.GUID);
            }
        }

        private bool MatchesViaGuids(out int resultMatchCount)
        {
            resultMatchCount = 0;
            for (int i = 0; i < query.Length; i++)
            {
                QuerySet qs = query[i];

                int slotCount = qs.Picker.Count;
                int validCount = 0;
                int matchCount = 0;
                // Each item counts once, like the bitmask path. A duplicate entry would otherwise pass AtLeast twice.
                pickerGuids.Clear();
                for (int j = 0; j < slotCount; j++)
                {
                    T socItem = qs.Picker[j];
                    if (!socItem)
                        continue;

                    if (!pickerGuids.Add(socItem.GUID))
                        continue;

                    validCount++;
                    if (targetGuids.Contains(socItem.GUID))
                        matchCount++;
                }

                resultMatchCount += matchCount;

                if (validCount == 0)
                    continue;

                if (!CollectionItemQueryRule.Passes((int)qs.MatchType, matchCount, validCount, qs.MinimumCount))
                    return false;
            }

            return true;
        }

        public bool IsEmpty()
        {
            bool allEmpty = true;
            foreach (QuerySet qs in query)
            {
                if (qs.Picker.Count != 0)
                {
                    allEmpty = false;
                    break;
                }
            }

            return allEmpty;
        }
    }
}
