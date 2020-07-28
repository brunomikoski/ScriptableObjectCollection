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
            internal string staticGeneratedFileParentFolder;

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
            if (!TryGetSettingsForCollection(collection, out CollectionToSettings settings))
                return defaultFileType;

            return settings.generatedStaticFileGeneratorType;
        }

        public bool IsCollectionAutomaticallyLoaded(ScriptableObjectCollection collection)
        {
            if (!TryGetSettingsForCollection(collection, out CollectionToSettings settings))
                return true;

            return settings.isAutomaticallyLoaded;
        }

        
        public string GetStaticFileFolderForCollection(ScriptableObjectCollection collection)
        {
            if (!TryGetSettingsForCollection(collection, out CollectionToSettings settings))
                return StaticScriptsFolderPath;

            if (string.IsNullOrEmpty(settings.staticGeneratedFileParentFolder))
                return StaticScriptsFolderPath;

            return settings.staticGeneratedFileParentFolder;
        }
        
        public void SetCollectionAutomaticallyLoaded(ScriptableObjectCollection targetCollection,  bool isAutomaticallyLoaded)
        {
            CollectionToSettings settings = GetOrCreateSettingsForCollection(targetCollection);
            settings.isAutomaticallyLoaded = isAutomaticallyLoaded;
            ObjectUtility.SetDirty(this);
        }

        public void SetStaticFileGeneratorTypeForCollection(ScriptableObjectCollection targetCollection, GeneratedStaticFileType staticCodeGeneratorType)
        {
            CollectionToSettings settings = GetOrCreateSettingsForCollection(targetCollection);
            settings.generatedStaticFileGeneratorType = staticCodeGeneratorType;
            ObjectUtility.SetDirty(this);
        }

        public void SetStaticFileFolderForCollection(ScriptableObjectCollection targetCollection, string targetFolder)
        {
            CollectionToSettings settings = GetOrCreateSettingsForCollection(targetCollection);
            settings.staticGeneratedFileParentFolder = targetFolder;
            ObjectUtility.SetDirty(this);
        }

        private bool TryGetSettingsForCollection(ScriptableObjectCollection targetCollection,
            out CollectionToSettings settings)
        {
            settings = collectionsSettings.FirstOrDefault(toSettings => toSettings.collection == targetCollection);
            return settings != null;
        }
        
        private CollectionToSettings GetOrCreateSettingsForCollection(ScriptableObjectCollection targetCollection)
        {
            if (!TryGetSettingsForCollection(targetCollection, out CollectionToSettings settings))
            {
                settings = new CollectionToSettings {collection = targetCollection};
                collectionsSettings.Add(settings);
            }

            return settings;
        }
    }
}
