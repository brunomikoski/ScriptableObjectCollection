using System;
using System.Collections.Generic;
using BrunoMikoski.ScriptableObjectCollections.Core;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [DefaultExecutionOrder(-1000)]
    public class CollectionsRegistry : ResourceScriptableObjectSingleton<CollectionsRegistry>
    {
        [SerializeField]
        private List<ScriptableObjectCollection> collections = new List<ScriptableObjectCollection>();
        [SerializeField, HideInInspector]
        private List<string> collectionGUIDs = new List<string>();
        
        public bool IsKnowCollectionGUID(string guid)
        {
            ValidateCurrentGUIDs();
            return collectionGUIDs.Contains(guid);
        }

        public void RegisterCollection(ScriptableObjectCollection targetCollection)
        {
            if (collections.Contains(targetCollection))
                return;
            
            collections.Add(targetCollection);
            collectionGUIDs.Add(targetCollection.GUID);
        }

        public void UnregisterCollection(ScriptableObjectCollection targetCollection)
        {
            if (!collections.Contains(targetCollection))
                return;

            collections.Remove(targetCollection);
            collectionGUIDs.Remove(targetCollection.GUID);
        }

        private void ValidateCurrentGUIDs()
        {
            if (collectionGUIDs.Count != collections.Count)
            {
                ReloadCollections();
                return;
            }

            for (int i = 0; i < collectionGUIDs.Count; i++)
            {
                string guid = collectionGUIDs[i];
                bool guidFound = false;
                for (int j = 0; j < collections.Count; j++)
                {
                    ScriptableObjectCollection collection = collections[j];
                    if (string.Equals(collection.GUID, guid, StringComparison.Ordinal))
                    {
                        guidFound = true;
                        break;
                    }
                }

                if (!guidFound)
                {
                    ReloadCollections();
                    break;
                }
            }
        }

        public ScriptableObjectCollection GetCollectionByGUID(string guid)
        {
            for (int i = 0; i < collections.Count; i++)
            {
                if (string.Equals(collections[i].GUID, guid, StringComparison.Ordinal))
                    return collections[i];
            }

            return null;
        }
        
        public bool TryGetCollectionForType(Type targetCollectionType, out ScriptableObjectCollection scriptableObjectCollection)
        {
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection collection = collections[i];
                if (collection.GetCollectionType() == targetCollectionType)
                {
                    scriptableObjectCollection = collection;
                    return true;
                }
            }

            scriptableObjectCollection = null;
            return false;
        }
        
        public bool TryGetCollectionByGUID(string targetGUID, out ScriptableObjectCollection resultCollection)
        {
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection scriptableObjectCollection = collections[i];
                if (string.Equals(scriptableObjectCollection.GUID, targetGUID, StringComparison.Ordinal))
                {
                    resultCollection = scriptableObjectCollection;
                    return true;
                }
            }

            resultCollection = null;
            return false;
        }
        
        public void DeleteCollection(ScriptableObjectCollection collection)
        {
            if (Application.isPlaying)
                return;
            
            if (!collections.Remove(collection))
                return;
            collectionGUIDs.Remove(collection.GUID);
            
#if UNITY_EDITOR
            for (int i = collection.Items.Count - 1; i >= 0; i--)
                UnityEditor.AssetDatabase.DeleteAsset(UnityEditor.AssetDatabase.GetAssetPath(collection.Items[i]));
#endif
            ObjectUtility.SetDirty(this);
        }
        
        public void ReloadCollections()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return;
            
            string[] collectionsGUIDs = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(ScriptableObjectCollection)}");

            collections.Clear();
            collectionGUIDs.Clear();
            
            for (int i = 0; i < collectionsGUIDs.Length; i++)
            {
                string collectionGUID = collectionsGUIDs[i];

                ScriptableObjectCollection collection =
                    UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(
                        UnityEditor.AssetDatabase.GUIDToAssetPath(collectionGUID));

                if (collection == null)
                    continue;

                collection.RefreshCollection();
                collections.Add(collection);
                collectionGUIDs.Add(collection.GUID);
            }

            ObjectUtility.SetDirty(this);
#endif
        }
        
        public void PreBuildProcess()
        {
            RemoveNonAutomaticallyInitializedCollections();
            ObjectUtility.SetDirty(this);
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        public void RemoveNonAutomaticallyInitializedCollections()
        {
            for (int i = collections.Count - 1; i >= 0; i--)
            {
                ScriptableObjectCollection collection = collections[i];
                if (ScriptableObjectCollectionSettings.Instance.IsCollectionAutomaticallyLoaded(collection))
                    continue;

                collections.Remove(collection);
                collectionGUIDs.Remove(collection.GUID);
            }
        }

        public void PostBuildProcess()
        {
            ReloadCollections();
        }
    }
}

