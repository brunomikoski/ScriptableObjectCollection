using System;
using System.Collections.Generic;
using System.Linq;
using BrunoMikoski.ScriptableObjectCollections.Core;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public class ScriptableObjectCollectionSettings : ResourceScriptableObjectSingleton<ScriptableObjectCollectionSettings>
    {
        [Serializable]
        private class CollectionToSettings
        {
            [SerializeField]
            internal ScriptableObjectCollection collection;
            
            [SerializeField]
            internal GeneratedStaticFileType generatedStaticFileGeneratorType;

            [SerializeField]
            internal string staticGeneratedFileLocation;

            [SerializeField]
            internal bool isAutomaticallyLoaded;
        }
        
#if UNITY_EDITOR
        
        [SerializeField]
        private UnityEditor.DefaultAsset collectionAssetsFolder;
        public string CollectionAssetsFolderPath => UnityEditor.AssetDatabase.GetAssetPath(collectionAssetsFolder);

        [SerializeField]
        private UnityEditor.DefaultAsset collectionScriptsFolder;
        public string CollectionScriptsFolderPath => UnityEditor.AssetDatabase.GetAssetPath(collectionScriptsFolder);
        
        [SerializeField]
        private UnityEditor.DefaultAsset staticScriptsFolder;
        public string StaticScriptsFolderPath => UnityEditor.AssetDatabase.GetAssetPath(staticScriptsFolder);

#endif

        [SerializeField]
        private GeneratedStaticFileType defaultFileType = GeneratedStaticFileType.DirectAccess;
        
        [SerializeField]
        private List<CollectionToSettings> collectionsSettings = new List<CollectionToSettings>();

        public GeneratedStaticFileType GetStaticFileTypeForCollection(ScriptableObjectCollection collection)
        {
            CollectionToSettings settings =
                collectionsSettings.FirstOrDefault(toSettings => toSettings.collection == collection);

            return settings?.generatedStaticFileGeneratorType ?? defaultFileType;
        }

        public bool IsCollectionAutomaticallyLoaded(ScriptableObjectCollection collection)
        {
            CollectionToSettings settings =
                collectionsSettings.FirstOrDefault(toSettings => toSettings.collection == collection);

            return settings?.isAutomaticallyLoaded ?? true;
        }

        public void SetCollectionAutomaticallyLoaded(ScriptableObjectCollection targetCollection,  bool isAutomaticallyLoaded)
        {
            CollectionToSettings settings =
                collectionsSettings.FirstOrDefault(toSettings => toSettings.collection == targetCollection);

            if (settings == null)
                collectionsSettings.Add(new CollectionToSettings {collection = targetCollection, isAutomaticallyLoaded = isAutomaticallyLoaded});
            else
                settings.isAutomaticallyLoaded = isAutomaticallyLoaded;

            ObjectUtility.SetDirty(this);
        }

        public void SetStaticFileGeneratorTypeForCollection(ScriptableObjectCollection targetCollection, GeneratedStaticFileType staticCodeGeneratorType)
        {
            CollectionToSettings settings =
                collectionsSettings.FirstOrDefault(toSettings => toSettings.collection == targetCollection);

            if (settings == null)
                collectionsSettings.Add(new CollectionToSettings {collection = targetCollection, generatedStaticFileGeneratorType = staticCodeGeneratorType});
            else
                settings.generatedStaticFileGeneratorType = staticCodeGeneratorType;

            ObjectUtility.SetDirty(this);
        }
    }
}
