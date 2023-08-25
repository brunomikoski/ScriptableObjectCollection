using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public interface IScriptableObjectCollectionGeneratorBase
    {
    }

    public interface IScriptableObjectCollectionGenerator<CollectionType, TemplateType>
        : IScriptableObjectCollectionGeneratorBase
        where CollectionType : ScriptableObjectCollection
        where TemplateType : ItemTemplate
    {
        bool ShouldRemoveNonGeneratedItems { get; }

        void GetItemTemplates(List<TemplateType> templates, CollectionType collection);
    }

    public abstract class ItemTemplate
    {
        public string name;
    }
}
