using System;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public class ScriptableObjectCollectionItem : ScriptableObject, IComparable<ScriptableObjectCollectionItem>, ISOCItem
    {
        [SerializeField, HideInInspector]
        private LongGuid guid;
        public LongGuid GUID
        {
            get
            {
                if (guid.IsValid())
                    return guid;
                
                guid = LongGuid.NewGuid();
                return guid;
            }
        }

        [SerializeField, HideInInspector]
        private LongGuid collectionGUID;
        
        private ScriptableObjectCollection cachedScriptableObjectCollection;
        public ScriptableObjectCollection Collection
        {
            get
            {
                if (cachedScriptableObjectCollection == null)
                {
                    if (collectionGUID.IsValid())
                    {
                        cachedScriptableObjectCollection = CollectionsRegistry.Instance.GetCollectionByGUID(collectionGUID);
                    }
                    else
                    {
                        CollectionsRegistry.Instance.TryGetCollectionFromItemType(GetType(), out cachedScriptableObjectCollection);
                        if (cachedScriptableObjectCollection != null)
                        {
                            Debug.Log($"Collection Item ({this.name}) was missing the Collection GUID, assigned to {cachedScriptableObjectCollection.name}");
                            collectionGUID = cachedScriptableObjectCollection.GUID;
                            ObjectUtility.SetDirty(this);
                        }
                    }
                }
                
                return cachedScriptableObjectCollection;
            }
        }

        public void SetCollection(ScriptableObjectCollection collection)
        {
            cachedScriptableObjectCollection = collection;
            collectionGUID = cachedScriptableObjectCollection.GUID;
            ObjectUtility.SetDirty(this);
        }

        public int CompareTo(ScriptableObjectCollectionItem other)
        {
            return string.Compare(name, other.name, StringComparison.Ordinal);
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
        
        public void GenerateGUID()
        {
            guid = LongGuid.NewGuid();
        }
    }
}
