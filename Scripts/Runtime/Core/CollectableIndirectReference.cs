using System;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [Obsolete("CollectableIndirectReference is deprecated, use CollectionItemIndirectReference instead")]
    public abstract class CollectableIndirectReference<TObject> : CollectionItemIndirectReference<TObject>
    where TObject : ScriptableObjectCollectionItem { }
}
