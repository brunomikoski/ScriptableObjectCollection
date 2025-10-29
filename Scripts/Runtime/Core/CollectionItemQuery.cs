using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Picker
{
    [Serializable]
    public class CollectionItemQuery<T>  where T : ScriptableObject, ISOCItem
    {
        public enum MatchType
        {
            Any = 0,
            All = 1,
            NotAny = 2,
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

        public bool Matches(IEnumerable<T> targetItems, out int resultMatchCount)
        {
            resultMatchCount = 0;

            if (query.Length == 0)
                return true;

            for (int i = 0; i < query.Length; i++)
            {
                QuerySet querySet = query[i];

                int matchCount = 0;
                for (int j = 0; j < querySet.Picker.Count; j++)
                {
                    T socItem = querySet.Picker[j];
                    if (!socItem)
                    {
                        continue;
                    }

                    foreach (T item in targetItems)
                    {
                        if (!item)
                        {
                            continue;
                        }
                        if (socItem.GUID.Equals(item.GUID))
                        {
                            matchCount++;
                        }
                    }
                }

                resultMatchCount += matchCount;

                switch (querySet.MatchType)
                {
                    case MatchType.NotAny:
                    {
                        if (matchCount > 0)
                        {
                            return false;
                        }
                        break;
                    }
                    case MatchType.NotAll:
                    {
                        if(matchCount == querySet.Picker.Count)
                        {
                            return false;
                        }
                        break;
                    }
                    case MatchType.Any:
                    {
                        if (matchCount == 0)
                        {
                            return false;
                        }
                        break;
                    }
                    case MatchType.All:
                    {
                        if (matchCount < querySet.Picker.Count)
                        {
                            return false;
                        }
                        break;
                    }
                }
            }

            return resultMatchCount > 0;
        }
    }
}
