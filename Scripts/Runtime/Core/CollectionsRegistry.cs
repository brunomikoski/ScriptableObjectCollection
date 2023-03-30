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

        [SerializeField]
        private CollectionsSharedSettings collectionSettings = new CollectionsSharedSettings();
        public CollectionsSharedSettings CollectionSettings => collectionSettings;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            LoadOrCreateInstance<CollectionsRegistry>();
        }
#if UNITY_EDITOR
        private void OnEnable()
        {
            ValidateCollectionSettings();
        }

        private void ValidateCollectionSettings()
        {
            if (Application.isPlaying)
                return;

            collectionSettings.Validate();
        }
#endif

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

        public bool TryGetCollectionByGUID(LongGuid targetGUID, out ScriptableObjectCollection resultCollection)
        {
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection scriptableObjectCollection = collections[i];
                if (scriptableObjectCollection.GUID == targetGUID)
                {
                    resultCollection = scriptableObjectCollection;
                    return true;
                }
            }

            resultCollection = null;
            return false;
        }
        
        public bool TryGetCollectionByGUID<T>(LongGuid targetGUID, out ScriptableObjectCollection<T> resultCollection) where T : ScriptableObjectCollectionItem
        {
            if (TryGetCollectionByGUID(targetGUID, out ScriptableObjectCollection foundCollection))
            {
                resultCollection = foundCollection as ScriptableObjectCollection<T>;
                return true;
            }

            resultCollection = null;
            return false;
        }
        
        public void ReloadCollections()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return;

            collections.Clear();

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

                    if (collections.Contains(collection))
                        continue;

                    collection.RefreshCollection();
                    collections.Add(collection);
                    changed = true;
                }
            }

            if (changed)
            {
                ObjectUtility.SetDirty(this);
            }
#endif
        }

        public void PreBuildProcess()
        {
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

                if (collectionSettings.IsCollectionAutoLoaded(collection))
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

#if UNITY_EDITOR
        public void PrepareForPlayMode()
        {
            for (int i = 0; i < collections.Count; i++)
                collections[i].PrepareForPlayMode();
        }

        public void PrepareForEditorMode()
        {
            for (int i = 0; i < collections.Count; i++)
                collections[i].PrepareForEditorMode();
        }
#endif
    }
}

