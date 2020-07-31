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
            internal bool isAutomaticallyLoaded = true;

            [SerializeField] 
            internal bool overrideStaticFileLocation;
        }
        
#if UNITY_EDITOR
        
#pragma warning disable 0649
        [SerializeField]
        private UnityEditor.DefaultAsset defaultScriptableObjectsFolder;
        public string DefaultScriptableObjectsFolder => UnityEditor.AssetDatabase.GetAssetPath(defaultScriptableObjectsFolder);

        [SerializeField]
        private UnityEditor.DefaultAsset defaultScriptsFolder;
        public string DefaultScriptsFolder => UnityEditor.AssetDatabase.GetAssetPath(defaultScriptsFolder);
        
        [SerializeField]
        private UnityEditor.DefaultAsset defaultGeneratedCodeFolder;
        public string DefaultGeneratedCodeFolder => UnityEditor.AssetDatabase.GetAssetPath(defaultGeneratedCodeFolder);
#pragma warning restore 0649
#endif

        [SerializeField]
        private GeneratedStaticFileType defaultGenerator = GeneratedStaticFileType.DirectAccess;
        
        [SerializeField, HideInInspector]
        private List<CollectionToSettings> collectionsSettings = new List<CollectionToSettings>();

        public GeneratedStaticFileType GetStaticFileTypeForCollection(ScriptableObjectCollection collection)
        {
            if (!TryGetSettingsForCollection(collection, out CollectionToSettings settings))
                return defaultGenerator;

            return settings.generatedStaticFileGeneratorType;
        }

        public bool IsCollectionAutomaticallyLoaded(ScriptableObjectCollection collection)
        {
            if (!TryGetSettingsForCollection(collection, out CollectionToSettings settings))
                return true;

            return settings.isAutomaticallyLoaded;
        }
        
        public bool IsOverridingStaticFileLocation(ScriptableObjectCollection collection)
        {
            if (!TryGetSettingsForCollection(collection, out CollectionToSettings settings))
                return false;

            return settings.overrideStaticFileLocation;
        }

        public void SetOverridingStaticFileLocation(ScriptableObjectCollection collection, bool isOverriding)
        {
            CollectionToSettings settings = GetOrCreateSettingsForCollection(collection);
            settings.overrideStaticFileLocation = isOverriding;
            ObjectUtility.SetDirty(this);
        }
        
        public string GetStaticFileFolderForCollection(ScriptableObjectCollection collection)
        {
            if (!TryGetSettingsForCollection(collection, out CollectionToSettings settings))
                return DefaultGeneratedCodeFolder;

            if (!settings.overrideStaticFileLocation || string.IsNullOrEmpty(settings.staticGeneratedFileParentFolder))
                return DefaultGeneratedCodeFolder;

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
