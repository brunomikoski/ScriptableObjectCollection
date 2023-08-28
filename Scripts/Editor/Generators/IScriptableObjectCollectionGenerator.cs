using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public interface IScriptableObjectCollectionGeneratorBase
    {
        /// <summary>
        /// If specified, any items that do not match the returned item templates get removed.
        /// Return false if you want to generate items but allow people to manually add items of their own.
        /// </summary>
        bool ShouldRemoveNonGeneratedItems { get; }
    }

    /// <summary>
    /// Interface for classes that generate items for a Scriptable Object Collection.
    /// </summary>
    /// <typeparam name="CollectionType">The type of collection to generate items for.</typeparam>
    /// <typeparam name="TemplateType">The template class that represents items to add/update.</typeparam>
    public interface IScriptableObjectCollectionGenerator<CollectionType, TemplateType>
        : IScriptableObjectCollectionGeneratorBase
        where CollectionType : ScriptableObjectCollection
        where TemplateType : ItemTemplate
    {
        void GetItemTemplates(List<TemplateType> templates, CollectionType collection);
    }

    /// <summary>
    /// Base class for templates that represent which items there should be in a collection.
    /// </summary>
    public abstract class ItemTemplate
    {
        public string name;
    }

    public class ItemTemplate<T> : ItemTemplate where T: ISOCItem
    {
    }

}
