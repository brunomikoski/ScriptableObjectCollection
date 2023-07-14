using System;
using System.Collections.Generic;
using BrunoMikoski.ScriptableObjectCollections.Core;
using UnityEngine;
using UnityEngine.Scripting;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrunoMikoski.ScriptableObjectCollections
{
    [DefaultExecutionOrder(-1000)]
    [Preserve]
    public class CollectionsRegistry : ResourceScriptableObjectSingleton<CollectionsRegistry>
    {
        [SerializeField] 
        private List<ScriptableObjectCollection> collections = new List<ScriptableObjectCollection>();
        public IReadOnlyList<ScriptableObjectCollection> Collections => collections;
        
        [SerializeField]
        private bool autoSearchForCollections = true;
        public bool AutoSearchForCollections => autoSearchForCollections;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            LoadOrCreateInstance<CollectionsRegistry>();
        }

        public bool IsKnowCollection(ScriptableObjectCollection targetCollection)
        {
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection collection = collections[i];
                if (collection != null && collection.GUID == targetCollection.GUID)
                    return true;
            }

            return false;
        }

        public void RegisterCollection(ScriptableObjectCollection targetCollection)
        {
            if (collections.Contains(targetCollection))
                return;
            
            collections.Add(targetCollection);
            
            ObjectUtility.SetDirty(this);
        }

        public void UnregisterCollection(ScriptableObjectCollection targetCollection)
        {
            if (!collections.Contains(targetCollection))
                return;

            collections.Remove(targetCollection);
            
            ObjectUtility.SetDirty(this);
        }

        public bool TryGetCollectionByName<T>(string targetCollectionName, out ScriptableObjectCollection<T> resultCollection) where T: ScriptableObject, ISOCItem
        {
            if (TryGetCollectionByName(targetCollectionName, out ScriptableObjectCollection collection))
            {
                resultCollection = (ScriptableObjectCollection<T>) collection;
                return true;
            }

            resultCollection = null;
            return false;
        }

        public bool TryGetCollectionByName(string targetCollectionName, out ScriptableObjectCollection resultCollection)
        {
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection collection = collections[i];
                if (collection.name.Equals(targetCollectionName, StringComparison.Ordinal))
                {
                    resultCollection = collection;
                    return true;
                }
            }

            resultCollection = null;
            return false;
        }

        
        public List<T> GetAllCollectionItemsOfType<T>() where T : ScriptableObject, ISOCItem
        {
            List<T> result = new List<T>();
            List<ScriptableObject> items = GetAllCollectionItemsOfType(typeof(T));
            for (int i = 0; i < items.Count; i++)
            {
                ScriptableObject scriptableObjectCollectionItem = items[i];
                result.Add(scriptableObjectCollectionItem as T);
            }

            return result;
        }

        public List<ScriptableObject> GetAllCollectionItemsOfType(Type itemType)
        {
            List<ScriptableObject> results = new List<ScriptableObject>();
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection scriptableObjectCollection = collections[i];
                if (!itemType.IsAssignableFrom(scriptableObjectCollection.GetItemType()))
                    continue;

                results.AddRange(scriptableObjectCollection.Items);
            }

            return results;
        }

        public List<ScriptableObjectCollection> GetCollectionsByItemType<T>() where T : ScriptableObjectCollectionItem
        {
            return GetCollectionsByItemType(typeof(T));
        }

        public List<ScriptableObjectCollection> GetCollectionsByItemType(Type targetCollectionItemType)
        {
            List<ScriptableObjectCollection> result = new List<ScriptableObjectCollection>();

            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection scriptableObjectCollection = collections[i];
                if (scriptableObjectCollection.GetItemType().IsAssignableFrom(targetCollectionItemType))
                {
                    result.Add(scriptableObjectCollection);
                }
            }

            return result;
        }


        [Obsolete("Use GetCollectionByGUID(ULongGuid guid) is obsolete, please regenerate your static class")]
        public ScriptableObjectCollection GetCollectionByGUID(string guid)
        {
            throw new Exception("GetCollectionByGUID(ULongGuid guid) is obsolete, please regenerate your static class");
        }

        public ScriptableObjectCollection GetCollectionByGUID(LongGuid guid)
        {
            for (int i = 0; i < collections.Count; i++)
            {
                if (collections[i].GUID == guid)
                    return collections[i];
            }

            return null;
        }
        
        public bool TryGetCollectionOfType<T>(out T resultCollection) where T: ScriptableObjectCollection
        {
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection scriptableObjectCollection = collections[i];
                if (scriptableObjectCollection is T collectionT)
                {
                    resultCollection = collectionT;
                    return true;
                }
            }

            resultCollection = null;
            return false;
        }

        public bool TryGetCollectionFromItemType(Type targetType, out ScriptableObjectCollection scriptableObjectCollection)
        {
            List<ScriptableObjectCollection> possibleCollections = new List<ScriptableObjectCollection>();
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection collection = collections[i];
                if (collection.GetItemType() == targetType || collection.GetItemType().IsAssignableFrom(targetType))
                {
                    possibleCollections.Add(collection);
                }
            }

            if (possibleCollections.Count == 1)
            {
                scriptableObjectCollection = possibleCollections[0];
                return true;
            }

            scriptableObjectCollection = null;
            return false;
        }

        public bool TryGetCollectionFromItemType<TargetType>(out ScriptableObjectCollection<TargetType> scriptableObjectCollection) where TargetType : ScriptableObjectCollectionItem
        {
            if (TryGetCollectionFromItemType(typeof(TargetType), out ScriptableObjectCollection resultCollection))
            {
                scriptableObjectCollection = (ScriptableObjectCollection<TargetType>) resultCollection;
                return true;
            }

            scriptableObjectCollection = null;
            return false;
        }


        public bool TryGetCollectionByGUID<T>(LongGuid targetGUID, out T resultCollection) where T: ScriptableObjectCollection
        {
            if (targetGUID.IsValid())
            {
                for (int i = 0; i < collections.Count; i++)
                {
                    ScriptableObjectCollection scriptableObjectCollection = collections[i];
                    if (scriptableObjectCollection.GUID == targetGUID)
                    {
                        resultCollection = (T) scriptableObjectCollection;
                        return resultCollection != null;
                    }
                }
            }

            resultCollection = null;
            return false;
        }
        
        public bool TryGetCollectionByGUID<T>(LongGuid targetGUID, out ScriptableObjectCollection<T> resultCollection) where T : ScriptableObject, ISOCItem
        {
            if (targetGUID.IsValid())
            {
                if (TryGetCollectionByGUID(targetGUID, out ScriptableObjectCollection foundCollection))
                {
                    resultCollection = foundCollection as ScriptableObjectCollection<T>;
                    return true;
                }
            }

            resultCollection = null;
            return false;
        }

        public void ReloadCollections()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return;

            List<ScriptableObjectCollection> foundCollections  = new List<ScriptableObjectCollection>();

            bool changed = false;
            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<ScriptableObjectCollection>();
            for (int i = 0; i < types.Count; i++)
            {
                Type type = types[i];
                string[] typeGUIDs = AssetDatabase.FindAssets($"t:{type.Name}");

                for (int j = 0; j < typeGUIDs.Length; j++)
                {
                    string typeGUID = typeGUIDs[j];
                    ScriptableObjectCollection collection = 
                        AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(AssetDatabase.GUIDToAssetPath(typeGUID));

                    if (collection == null)
                        continue;

                    if (foundCollections.Contains(collection))
                        continue;

                    if (!collections.Contains(collection))
                        changed = true;
                    
                    collection.RefreshCollection();
                    foundCollections.Add(collection);
                }
            }

            if (changed)
            {
                ValidateCollections();
                collections = foundCollections;
                ObjectUtility.SetDirty(this);
            }
#endif
        }

        public void PreBuildProcess()
        {
            ReloadCollections();
            RemoveNonAutomaticallyInitializedCollections();
#if UNITY_EDITOR
            AssetDatabase.SaveAssets();
#endif
        }

        public void RemoveNonAutomaticallyInitializedCollections()
        {
#if UNITY_EDITOR
            for (int i = collections.Count - 1; i >= 0; i--)
            {
                ScriptableObjectCollection collection = collections[i];

                if (collection.AutomaticallyLoaded)
                    continue;

                collections.Remove(collection);
            }
            ObjectUtility.SetDirty(this);
#endif
        }

        public void PostBuildProcess()
        {
            ReloadCollections();
        }

        public void ValidateCollections()
        {
            for (int i = collections.Count - 1; i >= 0; i--)
            {
                ScriptableObjectCollection collectionA = collections[i];
                if (collectionA == null)
                {
                    collections.RemoveAt(i);
                    continue;
                }
                    
                for (int j = collections.Count - 1; j >= 0; j--)
                {
                    ScriptableObjectCollection collectionB = collections[j];


                    if (i == j)
                        continue;
                    
                    if (collectionA.GUID == collectionB.GUID)
                    {
                        collectionA.GenerateNewGUID();
                        Debug.LogWarning(
                            $"Found duplicated GUID between {collectionA} and {collectionB}, please run the validation again to make sure this is fixed");
                    }
                }

                for (int j = collectionA.Items.Count - 1; j >= 0; j--)
                {
                    ScriptableObject scriptableObjectA = collectionA.Items[j];
                    ISOCItem itemA = scriptableObjectA as ISOCItem;
                    
                    for (int k = 0; k < collectionA.Items.Count; k++)
                    {
                        ScriptableObject scriptableObjectB = collectionA.Items[k];
                        ISOCItem itemB = scriptableObjectB as ISOCItem;

                        if (j == k)
                            continue;
                        
                        if (itemA.GUID == itemB.GUID)
                        {
                            itemA.GenerateNewGUID();
                            Debug.LogWarning($"Found duplicated GUID between {itemA} and {itemB}, please run the validation again to make sure this is fixed");
                        }
                    }
                }
            }
        }

        public void SetAutoSearchForCollections(bool isOn)
        {
            autoSearchForCollections = isOn;
            ObjectUtility.SetDirty(this);
        }
    }
}

