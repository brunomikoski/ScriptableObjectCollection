using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [Serializable]
    public abstract class CollectionItemIndirectReference
    {
        [FormerlySerializedAs("collectableGUID")]
        [SerializeField]
        protected string collectionItemGUID;
        
        [SerializeField]
        protected string collectionGUID;
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

                if (CollectionsRegistry.Instance.TryGetCollectionByGUID(collectionGUID,
                    out ScriptableObjectCollection<TObject> collection))
                {
                    if (collection.TryGetItemByGUID(collectionItemGUID,
                        out ScriptableObjectCollectionItem item))
                    {
                        cachedRef = item as TObject;
                    }
                }

                return cachedRef;
            }
            set => FromCollectionItem(value);
        }

        /// <summary>
        /// Used for serializing, as the protected fields only work for Unity's serializer
        /// </summary>
        public string PairedGUID
        {
            get => collectionGUID + ":" + collectionItemGUID;
            set
            {
                string[] split = value.Split(':');
                if (split.Length == 2)
                {
                    collectionGUID = split[0];
                    collectionItemGUID = split[1];
                }
            }
        }

        public CollectionItemIndirectReference()
        {
        }

        public CollectionItemIndirectReference(TObject item)
        {
            FromCollectionItem(item);
        }

        [Obsolete("FromCollectable is deprecated, use FromCollectionItem instead")]
        public void FromCollectable(ScriptableObjectCollectionItem item)
        {
            FromCollectionItem(item);
        }

        public void FromCollectionItem(ScriptableObjectCollectionItem item)
        {
            collectionItemGUID = item.GUID;
            collectionGUID = item.Collection.GUID;
        }
    }
}
