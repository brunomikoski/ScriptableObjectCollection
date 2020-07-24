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
        private List<string> knowCollectionGUIDs = new List<string>();
        
        public bool IsKnowCollectionGUID(string guid)
        {
            ValidateCurrentGUIDs();
            return knowCollectionGUIDs.Contains(guid);
        }

        private void ValidateCurrentGUIDs()
        {
            if (knowCollectionGUIDs.Count != collections.Count)
            {
                ReloadCollections();
                return;
            }

            for (int i = 0; i < knowCollectionGUIDs.Count; i++)
            {
                string guid = knowCollectionGUIDs[i];
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
        
        
        public void RemoveCollection(ScriptableObjectCollection collection)
        {
            if (collections.Remove(collection))
                knowCollectionGUIDs.Remove(collection.GUID);
            
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
            knowCollectionGUIDs.Clear();
            
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
                knowCollectionGUIDs.Add(collection.GUID);
            }

            ObjectUtility.SetDirty(this);
#endif
        }

       
    }
}

