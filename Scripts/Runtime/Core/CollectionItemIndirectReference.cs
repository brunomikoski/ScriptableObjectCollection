using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [Serializable]
    public abstract class CollectionItemIndirectReference
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

    }

    [Serializable]
    public abstract class CollectionItemIndirectReference<TObject> : CollectionItemIndirectReference
        where TObject : ScriptableObjectCollectionItem
    {
        [NonSerialized]
        private TObject cachedRef;
        public TObject Ref
        {
            get
            {
                if (cachedRef != null)
                {
                    return cachedRef;
                }

                if (CollectionsRegistry.Instance.TryGetCollectionByGUID(CollectionGUID,
                    out ScriptableObjectCollection<TObject> collection))
                {
                    if (collection.TryGetItemByGUID(CollectionItemGUID, out ScriptableObject item))
                    {
                        cachedRef = item as TObject;
                    }
                }

                return cachedRef;
            }
            set => FromCollectionItem(value);
        }

        public CollectionItemIndirectReference()
        {
        }

        public CollectionItemIndirectReference(TObject item)
        {
            FromCollectionItem(item);
        }

        public void FromCollectionItem(ScriptableObjectCollectionItem item)
        {
            (long, long) collectionItemValues = item.GUID.GetValue();
            collectionItemGUIDValueA = collectionItemValues.Item1;
            collectionItemGUIDValueB = collectionItemValues.Item2;


            (long,long) collectionGUIDValues = item.Collection.GUID.GetValue();
            collectionGUIDValueA = collectionGUIDValues.Item1;
            collectionGUIDValueB = collectionGUIDValues.Item2;
        }
    }
}
