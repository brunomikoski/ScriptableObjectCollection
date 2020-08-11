using System;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [Serializable]
    public class CollectableIndirectReference
    {
#pragma warning disable 0649
        [SerializeField]
        private string name;
#pragma warning restore 0649
 
        [SerializeField]
        protected string collectableGUID = String.Empty;

        [SerializeField]
        protected string collectionGUID;
        
        public virtual void FromCollectable(CollectableScriptableObject collectableScriptableObject)
        {
            collectableGUID = collectableScriptableObject.GUID;
            collectionGUID = collectableScriptableObject.Collection.GUID;
        }
    }
    
    [Serializable]
    public class CollectableIndirectReference<TObject> : CollectableIndirectReference
        where TObject : CollectableScriptableObject
    {
#if UNITY_EDITOR
        [SerializeField]
        private TObject editorAsset;
#endif
        [NonSerialized]
        private TObject cachedRef;
        public TObject Ref
        {
            get
            {
                if (cachedRef != null)
                    return cachedRef;

                if (CollectionsRegistry.Instance.TryGetCollectionByGUID(collectableGUID,
                    out ScriptableObjectCollection<TObject> collection))
                {
                    if (collection.TryGetCollectableByGUID(collectionGUID,
                        out CollectableScriptableObject collectable))
                    {
                        cachedRef = collectable as TObject;
                    }
                }

                return cachedRef;
            }
        }

        public override void FromCollectable(CollectableScriptableObject collectableScriptableObject)
        {
            base.FromCollectable(collectableScriptableObject);
#if UNITY_EDITOR
            editorAsset = collectableScriptableObject as TObject;
#endif
        }
    }
}
