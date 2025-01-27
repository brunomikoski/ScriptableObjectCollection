using System;
using System.Collections.Generic;
using System.Linq;
using BrunoMikoski.ScriptableObjectCollections.Core;
using UnityEngine;
using UnityEngine.Scripting;
#if UNITY_EDITOR
using System.Text;
using UnityEditor;
#endif

namespace BrunoMikoski.ScriptableObjectCollections
{
    [DefaultExecutionOrder(-1000)]
    [Preserve]
    public class CollectionsRegistry : ResourceScriptableObjectSingleton<CollectionsRegistry>
    {
        private const string NON_AUTO_INITIALIZED_COLLECTIONS_KEY = "NON_AUTO_INITIALIZED_COLLECTIONS";

        [SerializeField]
        private List<ScriptableObjectCollection> collections = new List<ScriptableObjectCollection>();
        public IReadOnlyList<ScriptableObjectCollection> Collections => collections;
        
        [SerializeField, HideInInspector]
        private bool autoSearchForCollections;
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

            if (!collections.Remove(targetCollection))
                return;
            
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

        public List<ScriptableObject> GetAllCollectionItemsOfType(Type targetItemType)
        {
            List<ScriptableObject> results = new List<ScriptableObject>();
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection scriptableObjectCollection = collections[i];
                Type collectionItemType = scriptableObjectCollection.GetItemType();
                if (!targetItemType.IsAssignableFrom(collectionItemType))
                    continue;

                results.AddRange(scriptableObjectCollection.Items);
            }

            return results;
        }

        public bool TryGetCollectionsOfItemType(Type targetType, out List<ScriptableObjectCollection> results)
        {
            List<ScriptableObjectCollection> availables = new();
            int minDistance = int.MaxValue;

            for (int i = 0; i < Collections.Count; i++)
            {
                ScriptableObjectCollection collection = Collections[i];

                if (collection == null)
                    continue;

                Type itemType = collection.GetItemType();

                if (itemType == null)
                    continue;

                if (itemType == typeof(ISOCItem) || itemType == typeof(ScriptableObjectCollectionItem) || itemType.BaseType == null)
                    continue;

                if (!itemType.IsAssignableFrom(targetType))
                    continue;

                int distance = GetInheritanceDistance(targetType, itemType);
                if (distance < minDistance)
                {
                    availables.Clear();
                    availables.Add(collection);
                    minDistance = distance;
                }
                else if (distance == minDistance)
                {
                    availables.Add(collection);
                }
            }

            if (availables.Count == 0)
            {
                results = null;
                return false;
            }

            results = availables;
            return true;
        }

        private int GetInheritanceDistance(Type fromType, Type toType)
        {
            int distance = 0;
            Type currentType = fromType;
            while (currentType != null && currentType != toType)
            {
                currentType = currentType.BaseType;
                distance++;
            }
            if (currentType == toType)
                return distance;
            return int.MaxValue;
        }

        public bool TryGetCollectionsOfItemType<T>(out List<ScriptableObjectCollection<T>> results)
            where T : ScriptableObject, ISOCItem
        {
            Type targetType = typeof(T);

            if (TryGetCollectionsOfItemType(targetType, out List<ScriptableObjectCollection> collections))
            {
                results = collections.Cast<ScriptableObjectCollection<T>>().ToList();
                return true;
            }

            results = null;
            return false;
        }
        
        public bool TryGetCollectionsOfType<T>(out List<T> inputActionMapCollections, bool allowSubclasses = true) where T : ScriptableObjectCollection
        {
            List<T> result = new List<T>();
            Type targetType = typeof(T);
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection scriptableObjectCollection = collections[i];
                Type collectionType = scriptableObjectCollection.GetType();
                if (collectionType == targetType || (allowSubclasses && collectionType.IsSubclassOf(targetType)))
                    result.Add((T)scriptableObjectCollection);
            }

            inputActionMapCollections = result;
            return result.Count > 0;
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
                if (collections[i] != null && collections[i].GUID == guid)
                    return collections[i];
            }

            return null;
        }
        
        public bool TryGetCollectionOfType(Type type, out ScriptableObjectCollection resultCollection)
        {
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection scriptableObjectCollection = collections[i];
                if (scriptableObjectCollection == null)
                {
                    ValidateCollections();
                    resultCollection = null;
                    return false;
                }
                if (scriptableObjectCollection.GetType() == type)
                {
                    resultCollection = scriptableObjectCollection;
                    return true;
                }
            }

            resultCollection = null;
            return false;
        }
        
        public bool TryGetCollectionOfType<T>(out T resultCollection) where T: ScriptableObjectCollection
        {
            bool didFind = TryGetCollectionOfType(typeof(T), out ScriptableObjectCollection baseCollection);
            resultCollection = baseCollection as T;
            return didFind;
        }
        
        public bool TryGetCollectionFromItemType(Type targetType, out ScriptableObjectCollection resultCollection)
        {
            if (TryGetCollectionsOfItemType(targetType, out List<ScriptableObjectCollection> possibleCollections))
            {
                if (possibleCollections.Count == 1)
                {
                    resultCollection = possibleCollections[0];
                    return true;
                }
            }

            resultCollection = null;
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
        
        public bool TryGetCollectionByGUID<T>(LongGuid targetGUID, out ScriptableObjectCollection resultCollection) where T : ScriptableObject, ISOCItem
        {
            if (targetGUID.IsValid())
            {
                if (TryGetCollectionByGUID(targetGUID, out ScriptableObjectCollection foundCollection))
                {
                    resultCollection = foundCollection as ScriptableObjectCollection;
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
            string[] typeGUIDs = AssetDatabase.FindAssets($"t:{nameof(ScriptableObjectCollection)}");

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
            StringBuilder removedAssetPaths = new StringBuilder();
            bool dirty = false;
            for (int i = collections.Count - 1; i >= 0; i--)
            {
                ScriptableObjectCollection collection = collections[i];

                if (collection.AutomaticallyLoaded)
                    continue;

                collections.Remove(collection);
                removedAssetPaths.Append($"{AssetDatabase.GetAssetPath(collection)}|");

                dirty = true;
            }

            if (dirty)
            {
                EditorPrefs.SetString(NON_AUTO_INITIALIZED_COLLECTIONS_KEY, removedAssetPaths.ToString());
                ObjectUtility.SetDirty(this);
            }
            else
            {
                EditorPrefs.DeleteKey(NON_AUTO_INITIALIZED_COLLECTIONS_KEY);
            }
#endif
        }

        public void ReloadUnloadedCollectionsIfNeeded()
        {
#if UNITY_EDITOR
            string removedAssetPaths = EditorPrefs.GetString(NON_AUTO_INITIALIZED_COLLECTIONS_KEY, string.Empty);
            if (string.IsNullOrEmpty(removedAssetPaths))
                return;

            string[] paths = removedAssetPaths.Split('|', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                ScriptableObjectCollection collection = AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(path);
                if (collection == null)
                    continue;

                collections.Add(collection);
            }

            EditorPrefs.DeleteKey(NON_AUTO_INITIALIZED_COLLECTIONS_KEY);
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
                if (collections[i] == null)
                    collections.RemoveAt(i);
            }

            for (int i = collections.Count - 1; i >= 0; i--)
            {
                ScriptableObjectCollection collectionA = collections[i];
                    
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
            if (isOn == autoSearchForCollections)
                return;
            
            autoSearchForCollections = isOn;
            ObjectUtility.SetDirty(this);
        }

        public void UpdateAutoSearchForCollections()
        {
            for (int i = 0; i < Collections.Count; i++)
            {
                ScriptableObjectCollection collection = Collections[i];
                if (!collection.AutomaticallyLoaded)
                {
                    SetAutoSearchForCollections(true);
                    return;
                }
            }

            SetAutoSearchForCollections(false);
        }
    }
}