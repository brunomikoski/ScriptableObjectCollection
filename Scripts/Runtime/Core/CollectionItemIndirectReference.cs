using System;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [Serializable]
    public abstract class CollectionItemIndirectReference
    {
        [SerializeField]
        protected string collectableGUID;
        
        [SerializeField]
        protected string collectionGUID;
    }

    [Serializable]
    public abstract class CollectionItemIndirectReference<TObject> : CollectionItemIndirectReference
        where TObject : ScriptableObjectCollectionItem
    {
        [NonSerialized]
        private TObject cachedRef;
        
        /// <summary>
        /// Alternative to [XmlIgnore] [JsonIgnore]
        /// </summary>
        /// <returns>false, because of a circular reference and the SO is not serializable</returns>
        public bool ShouldSerializeRef() => false;

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
                    if (collection.TryGetCollectableByGUID(collectableGUID,
                        out ScriptableObjectCollectionItem collectable))
                    {
                        cachedRef = collectable as TObject;
                    }
                }

                return cachedRef;
            }
            set => FromCollectable(value);
        }

        /// <summary>
        /// Used for serializing, as the protected fields only work for Unity's serializer
        /// </summary>
        public string PairedGUID
        {
            get => collectionGUID + ":" + collectableGUID;
            set
            {
                var split = value.Split(':');
                if (split.Length == 2)
                {
                    collectionGUID = split[0];
                    collectableGUID = split[1];
                }
            }
        }

        public CollectionItemIndirectReference()
        {
        }

        public CollectionItemIndirectReference(TObject collectableScriptableObject)
        {
            FromCollectable(collectableScriptableObject);
        }

        public void FromCollectable(ScriptableObjectCollectionItem scriptableObjectCollectionItem)
        {
            collectableGUID = scriptableObjectCollectionItem.GUID;
            collectionGUID = scriptableObjectCollectionItem.Collection.GUID;
        }
    }
}
