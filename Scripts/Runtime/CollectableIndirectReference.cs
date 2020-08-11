using System;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [Serializable]
    public abstract class CollectableIndirectReference
    {
#pragma warning disable 0649
        [SerializeField]
        private string name;
#pragma warning restore 0649
 
        [SerializeField]
        protected string collectableGUID;

        [SerializeField]
        protected string collectionGUID;
    }                                                                      
    
    [Serializable]
    public abstract class CollectableIndirectReference<TObject> : CollectableIndirectReference
        where TObject : CollectableScriptableObject
    {
        [NonSerialized]
        private TObject cachedRef;
        public TObject Ref
        {
            get
            {
                if (cachedRef != null)
                    return cachedRef;

                if (CollectionsRegistry.Instance.TryGetCollectionByGUID(collectionGUID,
                    out ScriptableObjectCollection<TObject> collection))
                {
                    if (collection.TryGetCollectableByGUID(collectableGUID,
                        out CollectableScriptableObject collectable))
                    {
                        cachedRef = collectable as TObject;
                    }
                }

                return cachedRef;
            }
        }

        public void FromCollectable(CollectableScriptableObject collectableScriptableObject)
        {
            collectableGUID = collectableScriptableObject.GUID;
            collectionGUID = collectableScriptableObject.Collection.GUID;
        }
    }
}
