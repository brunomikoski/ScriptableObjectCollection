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

        [NonSerialized]
        private List<T> targetBuffer = new List<T>();

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
        /// <param name="targetItems">The items to test against (e.g., the tags on a rigidbody). A null collection is treated as empty, so positive sets (<see cref="MatchType.Any"/>/<see cref="MatchType.All"/>/<see cref="MatchType.AtLeast"/>) fail and negative sets (<see cref="MatchType.NotAny"/>/<see cref="MatchType.NotAll"/>) pass.</param>
        /// <param name="resultMatchCount">Total number of individual picker items found across all query sets. Informational only (and partial when the method early-returns false); does not affect the return value.</param>
        public bool Matches(IEnumerable<T> targetItems, out int resultMatchCount)
        {
            resultMatchCount = 0;
            if (query.Length == 0)
                return true;

            targetBuffer.Clear();
            if (targetItems != null)
                targetBuffer.AddRange(targetItems);

            bool matches = Matches((IReadOnlyList<T>)targetBuffer, out resultMatchCount);
            targetBuffer.Clear();
            return matches;
        }

        public bool Matches(IReadOnlyList<T> targetItems, out int resultMatchCount)
        {
            resultMatchCount = 0;
            for (int i = 0; i < query.Length; i++)
            {
                QuerySet qs = query[i];
                CollectionItemPicker<T> picker = qs.Picker;

                int slotCount = picker.Count;
                int validCount = 0;
                int matchCount = 0;
                for (int j = 0; j < slotCount; j++)
                {
                    T socItem = picker[j];
                    if (!socItem)
                        continue;

                    LongGuid guid = socItem.GUID;
                    // Each item counts once. A duplicate entry would otherwise pass AtLeast twice.
                    if (IsPickedBefore(picker, j, guid))
                        continue;

                    validCount++;
                    if (ContainsGuid(targetItems, guid))
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

        private static bool IsPickedBefore(CollectionItemPicker<T> picker, int slot, LongGuid guid)
        {
            for (int i = 0; i < slot; i++)
            {
                T earlierItem = picker[i];
                if (!earlierItem)
                    continue;

                if (earlierItem.GUID == guid)
                    return true;
            }

            return false;
        }

        private static bool ContainsGuid(IReadOnlyList<T> items, LongGuid guid)
        {
            if (items == null)
                return false;

            for (int i = 0; i < items.Count; i++)
            {
                T item = items[i];
                if (!item)
                    continue;

                if (item.GUID == guid)
                    return true;
            }

            return false;
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
