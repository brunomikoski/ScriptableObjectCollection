using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [Serializable]
    public abstract class CollectionItemIndirectReference
    {
        [SerializeField]
        protected ulong collectionItemGUIDValueA;
        [SerializeField]
        protected ulong collectionItemGUIDValueB;

        protected ULongGuid CollectionItemGUID => new ULongGuid(collectionItemGUIDValueA, collectionItemGUIDValueB);


        [SerializeField]
        protected ulong collectionGUIDValueA;
        [SerializeField]
        protected ulong collectionGUIDValueB;

        protected ULongGuid CollectionGUID => new ULongGuid(collectionGUIDValueA, collectionGUIDValueB);

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
            (ulong, ulong) collectionItemValues = item.GUID.GetValue();
            collectionItemGUIDValueA = collectionItemValues.Item1;
            collectionItemGUIDValueB = collectionItemValues.Item2;


            (ulong, ulong) collectionGUIDValues = item.Collection.GUID.GetValue();
            collectionGUIDValueA = collectionGUIDValues.Item1;
            collectionGUIDValueB = collectionGUIDValues.Item2;
        }
    }
}
