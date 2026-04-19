using System;
using System.Collections.Generic;
using System.Linq;
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
            Any = 0,
            /// <summary>Target has every one of the picker items.</summary>
            All = 1,
            /// <summary>Target has none of the picker items (all picker items are absent).</summary>
            NotAny = 2,
            /// <summary>Target is missing at least one of the picker items (not all are present).</summary>
            NotAll = 3,
        }

        [Serializable]
        public class QuerySet
        {
            [SerializeField]
            private MatchType matchType;
            public MatchType MatchType => matchType;

            [SerializeField]
            private CollectionItemPicker<T> picker;
            public CollectionItemPicker<T> Picker => picker;

            public override string ToString()
            {
                return string.Join(", ", picker.Select(o => o.name));
            }
        }

        [SerializeField]
        private QuerySet[] query = Array.Empty<QuerySet>();

        private HashSet<LongGuid> targetGuids = new HashSet<LongGuid>(128);

        public bool Matches(params T[] targetItems)
        {
            return Matches(targetItems, out _);
        }

        public bool Matches(IEnumerable<T> targetItems)
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
                stringBuilder.Append(querySet);
                stringBuilder.Append(" ");
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Evaluates every <see cref="QuerySet"/> in the query against <paramref name="targetItems"/>.
        /// Returns <c>true</c> only if every set passes its <see cref="MatchType"/> check;
        /// an empty query returns <c>true</c>.
        /// </summary>
        /// <param name="targetItems">The items to test against (e.g., the tags on a rigidbody).</param>
        /// <param name="resultMatchCount">Total number of individual picker items found across all query sets. Informational only; does not affect the return value.</param>
        public bool Matches(IEnumerable<T> targetItems, out int resultMatchCount)
        {
            targetGuids.Clear();
            foreach (T item in targetItems)
            {
                if (item) 
                    targetGuids.Add(item.GUID);
            }

            resultMatchCount = 0;
            if (query.Length == 0)
                return true;

            for (int i = 0; i < query.Length; i++)
            {
                QuerySet qs = query[i];

                int pickerCount = qs.Picker.Count;
                int matchCount = 0;
                for (int j = 0; j < pickerCount; j++)
                {
                    T socItem = qs.Picker[j];
                    if (!socItem) 
                        continue;
                    
                    if (targetGuids.Contains(socItem.GUID))
                        matchCount++;
                }

                resultMatchCount += matchCount;

                switch (qs.MatchType)
                {
                    case MatchType.NotAny:
                    {
                        if (matchCount > 0)
                            return false;
                        break;
                    }
                    case MatchType.NotAll:
                    {
                        if (matchCount == pickerCount)
                            return false;
                        break;
                    }
                    case MatchType.Any:
                    {
                        if (matchCount == 0) 
                            return false; 
                        break;
                    }
                    case MatchType.All:
                    {
                        if (matchCount < pickerCount) 
                            return false; 
                        break;

                    }
                }
            }
            
            return true;
        }
    }
}
