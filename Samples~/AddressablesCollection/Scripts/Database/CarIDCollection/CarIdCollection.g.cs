//  Automatically generated
//

using BrunoMikoski.ScriptableObjectCollections;
using BrunoMikoski.Templates;
using System.Collections.Generic;
using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace BrunoMikoski.Templates
{
    public partial class CarID
    {
        private static bool hasCachedValues;
        private static CarIDCollection cachedValues;
        
        private static bool hasCachedAmbulance;
        private static BrunoMikoski.Templates.CarID cachedAmbulance;
        private static bool hasCachedDelivery;
        private static BrunoMikoski.Templates.CarID cachedDelivery;
        private static bool hasCachedFiretruck;
        private static BrunoMikoski.Templates.CarID cachedFiretruck;
        
        public static BrunoMikoski.Templates.CarIDCollection Values
        {
            get
            {
                if (!hasCachedValues)
                    hasCachedValues = CollectionsRegistry.Instance.TryGetCollectionByGUID(new LongGuid(5677458002784623978, -2514090179870802282), out cachedValues);
                return cachedValues;
            }
        }
        
        
        public static BrunoMikoski.Templates.CarID Ambulance
        {
            get
            {
                if (!hasCachedAmbulance)
                    hasCachedAmbulance = Values.TryGetItemByGUID(new LongGuid(4970954985142910969, 9096795907161232567), out cachedAmbulance);
                return cachedAmbulance;
            }
        }
        
        public static BrunoMikoski.Templates.CarID Delivery
        {
            get
            {
                if (!hasCachedDelivery)
                    hasCachedDelivery = Values.TryGetItemByGUID(new LongGuid(5654874434753296594, -432112441827506813), out cachedDelivery);
                return cachedDelivery;
            }
        }
        
        public static BrunoMikoski.Templates.CarID Firetruck
        {
            get
            {
                if (!hasCachedFiretruck)
                    hasCachedFiretruck = Values.TryGetItemByGUID(new LongGuid(5173197227317346651, -2268809456471727447), out cachedFiretruck);
                return cachedFiretruck;
            }
        }
        
        
        public static bool IsCollectionLoaded()
        {
            return Values != null;
        }
        
        private static AsyncOperationHandle<BrunoMikoski.Templates.CarIDCollection> collectionHandle;
        public static AsyncOperationHandle<BrunoMikoski.Templates.CarIDCollection> LoadCollectionAsync()
        {
            collectionHandle = Addressables.LoadAssetAsync<BrunoMikoski.Templates.CarIDCollection>("d6dc9ffb2365b66448ce88ff63f60b6d");
            collectionHandle.Completed += operation =>
            {
                CollectionsRegistry.Instance.RegisterCollection(operation.Result);
                hasCachedValues = true;
                cachedValues = operation.Result;
            };
            return collectionHandle;
        }
        
        public static void UnloadCollection()
        {
            CollectionsRegistry.Instance.UnregisterCollection(Values);
            hasCachedValues = false;
            cachedValues = null;
            Addressables.Release(collectionHandle);
        }
    }
}
