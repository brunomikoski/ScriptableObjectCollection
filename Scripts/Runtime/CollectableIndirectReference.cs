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
        protected string collectableGUID = String.Empty;

        [SerializeField]
        protected string collectionGUID;
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
        private TObject cachedITem;
        public new TObject Item
        {
            get
            {
                if (cachedITem != null)
                    return cachedITem;

                if (CollectionsRegistry.Instance.TryGetCollectionByGUID(collectableGUID,
                    out ScriptableObjectCollection<TObject> collection))
                {
                    if (collection.TryGetCollectableByGUID(collectionGUID,
                        out CollectableScriptableObject collectable))
                    {
                        cachedITem = collectable as TObject;
                    }
                }

                return cachedITem;
            }
        }
    }
}
