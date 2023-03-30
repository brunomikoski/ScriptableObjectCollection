using System;
using System.Collections.Generic;
using BrunoMikoski.ScriptableObjectCollections.Core;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [Serializable]
    public class CollectionsSharedSettings 
    {
        [Serializable]
        public class CollectionSettingsData
        {
            [SerializeField]
            private LongGuid collectionGUID;
            public LongGuid CollectionGuid => collectionGUID;

            [SerializeField]
            private bool automaticallyLoaded = true;
            public bool AutomaticallyLoaded => automaticallyLoaded;

            [SerializeField]
            private bool generateAsPartialClass = true;
            public bool GenerateAsPartialClass => generateAsPartialClass;

            [SerializeField]
            private bool generateAsBaseClass;
            public bool GenerateAsBaseClass => generateAsBaseClass;

            [SerializeField]
            private string generatedFileLocationPath;
            public string GeneratedFileLocationPath => generatedFileLocationPath;

            [SerializeField]
            private string generatedStaticClassFileName;
            public string GeneratedStaticClassFileName => generatedStaticClassFileName;

            [SerializeField]
            private string generateStaticFileNamespace;
            public string GenerateStaticFileNamespace => generateStaticFileNamespace;

            public CollectionSettingsData(LongGuid collectionGuid)
            {
                collectionGUID = collectionGuid;
            }
            
            public void SetAutomaticallyLoaded(bool autoLoaded)
            {
                automaticallyLoaded = autoLoaded;
            }

            public void SetGenerateAsPartialClass(bool partialClass)
            {
                generateAsPartialClass = partialClass;
            }
            
            public void SetGenerateAsBaseClass(bool baseClass)
            {
                generateAsBaseClass = baseClass;
            }
            
            public void SetGeneratedFileLocationPath(string locationPath)
            {
                generatedFileLocationPath = locationPath;
            }
            
            public void SetGeneratedStaticClassFileName(string fileName)
            {
                generatedStaticClassFileName = fileName;
            }
            
            public void SetGenerateStaticFileNamespace(string fileNamespace)
            {
                generateStaticFileNamespace = fileNamespace;
            }
        }


        [SerializeField]
        private CollectionSettingsData[] collectionsSettings = new CollectionSettingsData[0];

        [SerializeField]
        private string generatedScriptsDefaultFilePath = "Assets\\Generated\\Scripts";
        public string GeneratedScriptsDefaultFilePath => generatedScriptsDefaultFilePath;

        public bool IsCollectionAutoLoaded(ScriptableObjectCollection targetCollection)
        {
            CollectionSettingsData collectionSettings = GetOrCreateCollectionSettings(targetCollection);
            return collectionSettings.AutomaticallyLoaded;
        }
        
        public bool IsCollectionGenerateAsPartialClass(ScriptableObjectCollection targetCollection)
        {
            CollectionSettingsData collectionSettings = GetOrCreateCollectionSettings(targetCollection);
            return collectionSettings.GenerateAsPartialClass;
        }
        
        public bool IsCollectionGenerateAsBaseClass(ScriptableObjectCollection targetCollection)
        {
            CollectionSettingsData collectionSettings = GetOrCreateCollectionSettings(targetCollection);
            return collectionSettings.GenerateAsBaseClass;
        }
        
        public string GetCollectionGeneratedFileLocationPath(ScriptableObjectCollection targetCollection)
        {
            CollectionSettingsData collectionSettings = GetOrCreateCollectionSettings(targetCollection);
            return collectionSettings.GeneratedFileLocationPath;
        }
        
        public string GetCollectionGeneratedStaticClassFileName(ScriptableObjectCollection targetCollection)
        {
            CollectionSettingsData collectionSettings = GetOrCreateCollectionSettings(targetCollection);
            return collectionSettings.GeneratedStaticClassFileName;
        }
        
        public string GetCollectionGeneratedStaticFileNamespace(ScriptableObjectCollection targetCollection)
        {
            CollectionSettingsData collectionSettings = GetOrCreateCollectionSettings(targetCollection);
            return collectionSettings.GenerateStaticFileNamespace;
        }
        
        public void SetCollectionAutoLoaded(ScriptableObjectCollection targetCollection, bool autoLoaded)
        {
            CollectionSettingsData collectionSettings = GetOrCreateCollectionSettings(targetCollection);
            collectionSettings.SetAutomaticallyLoaded(autoLoaded);
            SetDirty();
        }
        
        public void SetGenerateAsPartialClass(ScriptableObjectCollection targetCollection, bool generateAsPartialClass)
        {
            CollectionSettingsData collectionSettings = GetOrCreateCollectionSettings(targetCollection);
            collectionSettings.SetGenerateAsPartialClass(generateAsPartialClass);
            SetDirty();
        }
        
        public void SetCollectionGenerateAsBaseClass(ScriptableObjectCollection targetCollection, bool generateAsBaseClass)
        {
            CollectionSettingsData collectionSettings = GetOrCreateCollectionSettings(targetCollection);
            collectionSettings.SetGenerateAsBaseClass(generateAsBaseClass);
            SetDirty();
        }
        
        public void SetCollectionGeneratedFileLocationPath(ScriptableObjectCollection targetCollection, string locationPath)
        {
            if (string.IsNullOrEmpty(generatedScriptsDefaultFilePath) && !string.IsNullOrEmpty(locationPath))
                generatedScriptsDefaultFilePath = locationPath;
            
            CollectionSettingsData collectionSettings = GetOrCreateCollectionSettings(targetCollection);
            collectionSettings.SetGeneratedFileLocationPath(locationPath);
            SetDirty();
        }
        
        public void SetCollectionGeneratedStaticClassFileName(ScriptableObjectCollection targetCollection, string fileName)
        {
            CollectionSettingsData collectionSettings = GetOrCreateCollectionSettings(targetCollection);
            collectionSettings.SetGeneratedStaticClassFileName(fileName);
            SetDirty();
        }
        
        public void SetCollectionGeneratedStaticFileNamespace(ScriptableObjectCollection targetCollection, string fileNamespace)
        {
            CollectionSettingsData collectionSettings = GetOrCreateCollectionSettings(targetCollection);
            collectionSettings.SetGenerateStaticFileNamespace(fileNamespace);
            SetDirty();
        }

        private CollectionSettingsData GetOrCreateCollectionSettings(ScriptableObjectCollection targetCollection)
        {
            if (TryGetCollectionSettings(targetCollection, out CollectionSettingsData collectionSettings))
                return collectionSettings;

            collectionSettings = new CollectionSettingsData(targetCollection.GUID);
            collectionSettings.SetGeneratedFileLocationPath(GeneratedScriptsDefaultFilePath);
            Array.Resize(ref collectionsSettings, collectionsSettings.Length + 1);
            collectionsSettings[collectionsSettings.Length - 1] = collectionSettings;
            SetDirty();
            
            return collectionSettings;
        }

        private void SetDirty()
        {
#if UNITY_EDITOR
            
            UnityEditor.EditorUtility.SetDirty(CollectionsRegistry.Instance);
#endif
        }

        private bool TryGetCollectionSettings(ScriptableObjectCollection targetCollection, out CollectionSettingsData collectionSettingsData)
        {
            if (collectionsSettings == null)
            {
                collectionSettingsData = null;
                return false;
            }
            
            for (int i = 0; i < collectionsSettings.Length; i++)
            {
                CollectionSettingsData collectionsSetting = collectionsSettings[i];
                if(collectionsSetting.CollectionGuid == targetCollection.GUID)
                {
                    collectionSettingsData = collectionsSetting;
                    return true;
                }
            }

            collectionSettingsData = null;
            return false;
        }

        public void SetGeneratedScriptsDefaultFilePath(string assetPath)
        {
            generatedScriptsDefaultFilePath = assetPath;
            SetDirty();
        }

        public void Validate()
        {
            List<CollectionSettingsData> validItems = new List<CollectionSettingsData>();
            bool foundInvalidItems = false;
            for (int i = collectionsSettings.Length - 1; i >= 0; i--)
            {
                CollectionSettingsData collectionSettingsData = collectionsSettings[i];
                if (CollectionsRegistry.Instance.TryGetCollectionByGUID(collectionSettingsData.CollectionGuid,out ScriptableObjectCollection collection))
                {
                    validItems.Add(collectionSettingsData);
                }
                else
                {
                     foundInvalidItems = true;
                }
            }

            if (foundInvalidItems)
            {
                collectionsSettings = validItems.ToArray();
                SetDirty();
            }
        }
    }
}