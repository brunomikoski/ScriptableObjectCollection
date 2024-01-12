using System;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public class ScriptableObjectCollectionItem : ScriptableObject, IComparable<ScriptableObjectCollectionItem>, ISOCItem, IEquatable<ISOCItem>
    {
        [SerializeField, HideInInspector]
        private LongGuid guid;
        public LongGuid GUID
        {
            get
            {
                if (guid.IsValid())
                    return guid;
                
                GenerateNewGUID();
                return guid;
            }
        }

        [SerializeField, HideInInspector]
        private LongGuid collectionGUID;


        private bool hasCachedCollection;
        private ScriptableObjectCollection cachedCollection;
        public ScriptableObjectCollection Collection
        {
            get
            {
                if (!hasCachedCollection)
                {
                    if (collectionGUID.IsValid())
                    {
                        cachedCollection = CollectionsRegistry.Instance.GetCollectionByGUID(collectionGUID);
                    }
                    else
                    {
                        CollectionsRegistry.Instance.TryGetCollectionFromItemType(GetType(), out cachedCollection);
                        if (cachedCollection != null)
                        {
                            collectionGUID = cachedCollection.GUID;
                            ObjectUtility.SetDirty(this);
                        }
                    }

                    hasCachedCollection = cachedCollection != null;
                }
                
                return cachedCollection;
            }
        }
        
        private bool didCacheIndex;
        private int cachedIndex;
        public int Index
        {
            get
            {
                if (!didCacheIndex)
                {
                    didCacheIndex = true;
                    cachedIndex = Collection.Items.IndexOf(this);
                }
                return cachedIndex;
            }
        }

        public void SetCollection(ScriptableObjectCollection collection)
        {
            cachedCollection = collection;
            collectionGUID = cachedCollection.GUID;
            ObjectUtility.SetDirty(this);
        }
        
        public void GenerateNewGUID()
        {
            guid = LongGuid.NewGuid();
            ObjectUtility.SetDirty(this);
        }

        public int CompareTo(ScriptableObjectCollectionItem other)
        {
            return string.Compare(name, other.name, StringComparison.Ordinal);
        }

        public bool Equals(ISOCItem other)
        {
            if (other == null)
                return false;

            return GUID == other.GUID;
        }

        public override bool Equals(object o)
        {
            ScriptableObjectCollectionItem other = o as ScriptableObjectCollectionItem;
            if (other == null)
                return false;

            return ReferenceEquals(this, other);
        }

        public static bool operator==(ScriptableObjectCollectionItem left, ScriptableObjectCollectionItem right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (ReferenceEquals(left, null))
                return false;

            if (ReferenceEquals(right, null))
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(ScriptableObjectCollectionItem left, ScriptableObjectCollectionItem right)
        {
            return !(left == right);
        }
        
        public override int GetHashCode()
        {
            return GUID.GetHashCode();
        }
    }
}
