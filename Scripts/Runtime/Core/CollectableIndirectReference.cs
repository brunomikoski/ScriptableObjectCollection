using System;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [Obsolete("CollectableIndirectReference have been renamed to CollectionItemIndirectReference")]
    public abstract class CollectableIndirectReference<TObject> : CollectionItemIndirectReference<TObject>
    where TObject : ScriptableObjectCollectionItem { }
}
