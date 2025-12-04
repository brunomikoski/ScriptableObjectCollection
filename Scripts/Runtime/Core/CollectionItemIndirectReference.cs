using System;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [Serializable]
    public abstract class CollectionItemIndirectReference: IEquatable<CollectionItemIndirectReference>, IEquatable<ISOCItem>
    {
        [SerializeField]
        protected long collectionItemGUIDValueA;
        [SerializeField]
        protected long collectionItemGUIDValueB;
        protected LongGuid CollectionItemGUID => new LongGuid(collectionItemGUIDValueA, collectionItemGUIDValueB);


        [SerializeField]
        protected long collectionGUIDValueA;
        [SerializeField]
        protected long collectionGUIDValueB;
        protected LongGuid CollectionGUID => new LongGuid(collectionGUIDValueA, collectionGUIDValueB);

        [SerializeField]
        protected string itemLastKnownName;
        [SerializeField]
        protected string collectionLastKnowName;

        public bool Equals(CollectionItemIndirectReference other)
        {
            if (other == null)
                return false;
            
            return CollectionGUID == other.CollectionGUID && CollectionItemGUID == other.CollectionItemGUID;
        }

        public bool Equals(ISOCItem other)
        {
            if (other == null)
                return false;
            
            return CollectionGUID == other.Collection.GUID && CollectionItemGUID == other.GUID;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            
            return Equals((CollectionItemIndirectReference) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CollectionItemGUID.GetHashCode(), CollectionGUID.GetHashCode());
        }

        public bool IsValid()
        {
            return CollectionItemGUID.IsValid() && CollectionGUID.IsValid();
        }
    }

    [Serializable]
    public class CollectionItemIndirectReference<TObject> : CollectionItemIndirectReference
        where TObject : ScriptableObject, ISOCItem
    {
        [NonSerialized]
        private TObject cachedRef;
        public TObject Ref
        {
            get
            {
                if (cachedRef != null)
                    return cachedRef;

                if (TryResolveReference(out TObject resultObj))
                    cachedRef = resultObj;
                
                return cachedRef;
            }
        }
        
        public static implicit operator TObject(CollectionItemIndirectReference<TObject> reference)
        {
            return reference?.Ref;
        }

        public static implicit operator ScriptableObjectCollectionItem(CollectionItemIndirectReference<TObject> reference)
        {
            return reference?.Ref as ScriptableObjectCollectionItem;
        }

        public static implicit operator CollectionItemIndirectReference<TObject>(TObject item)
        {
            return item == null ? null : new CollectionItemIndirectReference<TObject>(item);
        }

        private bool TryResolveReference(out TObject result)
        {
            if (CollectionsRegistry.Instance.TryGetCollectionByGUID(CollectionGUID, out ScriptableObjectCollection collection))
            {
                if (collection.TryGetItemByGUID(CollectionItemGUID, out ScriptableObject item))
                {
                    result = item as TObject;
                    return true;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(collectionLastKnowName))
                {
                    if (CollectionsRegistry.Instance.TryGetCollectionByName(collectionLastKnowName, out collection))
                    {
                        SetCollection(collection);

                        if (!string.IsNullOrEmpty(itemLastKnownName))
                        {
                            if(collection.TryGetItemByName(itemLastKnownName, out ScriptableObject possibleResult))
                            {
                                result = possibleResult as TObject;
                                if (result == null)
                                {
                                    return false;
                                }
                                SetCollectionItem(result);
                                return true;
                            }
                        }
                    }
                }
            }

            result = null;
            return false;
        }

        public CollectionItemIndirectReference()
        {
        }

        public CollectionItemIndirectReference(TObject item)
        {
            FromCollectionItem(item);
        }

        public void FromCollectionItem(ISOCItem item)
        {
            SetCollectionItem(item);
            SetCollection(item.Collection);
        }

        private void SetCollectionItem(ISOCItem item)
        {
            (long, long) collectionItemValues = item.GUID.GetRawValues();
            collectionItemGUIDValueA = collectionItemValues.Item1;
            collectionItemGUIDValueB = collectionItemValues.Item2;
            itemLastKnownName = item.name;
        }

        public void SetCollection(ScriptableObjectCollection targetCollection)
        {
            (long,long) collectionGUIDValues = targetCollection.GUID.GetRawValues();
            collectionGUIDValueA = collectionGUIDValues.Item1;
            collectionGUIDValueB = collectionGUIDValues.Item2;
            collectionLastKnowName = targetCollection.name;
        }
    }
}
