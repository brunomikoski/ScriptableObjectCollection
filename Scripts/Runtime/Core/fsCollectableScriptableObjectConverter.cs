/*
using System;
using FullSerializer;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public class fsCollectableScriptableObjectConverter
    {
        [fsConverterRegistrar]
        public class fsEnumItemConverter : fsConverter
        {
            public override bool CanProcess(Type type)
            {
                return typeof(ScriptableObjectCollectionItem).IsAssignableFrom(type);
            }

            public override bool RequestCycleSupport(Type storageType)
            {
                return false;
            }

            public override bool RequestInheritanceSupport(Type storageType)
            {
                return true;
            }

            public override object CreateInstance(fsData data, Type storageType)
            {
                return null;
            }

            public override fsResult TrySerialize(object instance, out fsData serialized, Type storageType)
            {
                ScriptableObjectCollectionItem item = instance as ScriptableObjectCollectionItem;

                serialized = fsData.CreateDictionary();

                if (item == null)
                    return fsResult.Success;

                if (item.Collection == null)
                    return fsResult.Fail("No Collection Found");

                serialized.AsDictionary.Add("Collection", new fsData(item.Collection.GUID));
                serialized.AsDictionary.Add("Guid", new fsData(item.GUID));

                return fsResult.Success;
            }

            public override fsResult TryDeserialize(fsData data, ref object instance, Type storageType)
            {
                if (!data.IsDictionary)
                {
                    return fsResult.Fail("Data is not a dictionary");
                }

                string parentCollectionGUID = data.AsDictionary["Collection"].AsString;
                if (string.IsNullOrEmpty(parentCollectionGUID))
                    return fsResult.Fail("Parent collection guid is null or empty");

                if (!CollectionsRegistry.Instance.TryGetCollectionByGUID(parentCollectionGUID,
                    out ScriptableObjectCollection collection))
                {
                    return fsResult.Fail($"Cannot find collection with guid {parentCollectionGUID}");
                }

                string itemGUID = data.AsDictionary["Guid"].AsString;
                if (string.IsNullOrEmpty(itemGUID))
                    return fsResult.Fail("item guid is null or empty");

                if (!collection.TryGetCollectableByGUID(itemGUID, out ScriptableObjectCollectionItem collectable))
                {
                    return fsResult.Fail(
                        $"Cannot find collectable with guid {itemGUID} inside collection {collection}");
                }

                instance = collectable;
                return fsResult.Success;
            }
        }
    }
}
*/
