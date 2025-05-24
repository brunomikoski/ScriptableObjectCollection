using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [Serializable]
    public class CollectionSettings
    {
        public LongGuid Guid;
        public string Namespace;
        public string StaticFilename;
        public string ParentFolderPath;
        public bool WriteAsPartialClass;
        public bool UseBaseClassForItems;
        public bool EnforceIndirectAccess;

        public bool WriteAddressableLoadingMethods;

        private AssetImporter importer;

        public CollectionSettings()
        {
        }

        public CollectionSettings(ScriptableObjectCollection targetCollection)
        {
            Guid = targetCollection.GUID;
            string targetNamespace = targetCollection.GetItemType().Namespace;
            if (string.IsNullOrEmpty(targetNamespace) && !string.IsNullOrEmpty(SOCSettings.Instance.NamespacePrefix))
                targetNamespace = $"{SOCSettings.Instance.NamespacePrefix}";
            
            Namespace = targetNamespace;

            if (!string.IsNullOrEmpty(SOCSettings.Instance.generatedScriptsDefaultFilePath) && AssetDatabase.IsValidFolder(SOCSettings.Instance.generatedScriptsDefaultFilePath))
            {
                ParentFolderPath = SOCSettings.Instance.generatedScriptsDefaultFilePath;
            }
            else
            {
                string baseClassPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(targetCollection));
                string parentFolder = Path.GetDirectoryName(baseClassPath);
                ParentFolderPath = parentFolder;
            }
            
            bool canBePartial = CodeGenerationUtility.CheckIfCanBePartial(targetCollection, ParentFolderPath);
            
            if (!canBePartial)
                StaticFilename = $"{targetCollection.GetType().Name}Static".FirstToUpper();
            else 
                StaticFilename = $"{targetCollection.GetType().Name}".FirstToUpper();

            WriteAsPartialClass = canBePartial;
            UseBaseClassForItems = false;
            EnforceIndirectAccess = false;
            Save();
        }
        
        public bool ShouldWriteAddressableLoadingMethods()
        {
            if (!CollectionsRegistry.Instance.GetCollectionByGUID(Guid).AutomaticallyLoaded)
                return false;

            return WriteAddressableLoadingMethods;
        }

        public void SetImporter(AssetImporter targetImporter)
        {
            importer = targetImporter;
        }

        public void Save()
        {
            if (importer == null)
                return;

            importer.userData = EditorJsonUtility.ToJson(this);
            AssetDatabase.WriteImportSettingsIfDirty(importer.assetPath);
        }

        public void SetWriteAddressableLoadingMethods(bool shouldWriteAddressablesMethods)
        {
            if (WriteAddressableLoadingMethods == shouldWriteAddressablesMethods)
                return;

            WriteAddressableLoadingMethods = shouldWriteAddressablesMethods;
            Save();
        }

        public void SetEnforceIndirectAccess(bool enforceIndirectAccess)
        {
            if (EnforceIndirectAccess == enforceIndirectAccess)
                return;

            EnforceIndirectAccess = enforceIndirectAccess;
            Save();
        }

        public void SetStaticFilename(string targetNewName)
        {
            if (string.Equals(StaticFilename, targetNewName, StringComparison.Ordinal))
                return;

            StaticFilename = targetNewName;
            Save();
        }

        public void SetNamespace(string targetNamespace)
        {
            if (string.Equals(Namespace, targetNamespace, StringComparison.Ordinal))
                return;

            Namespace = targetNamespace;
            Save();
        }

        public void SetWriteAsPartialClass(bool writeAsPartial)
        {
            if (WriteAsPartialClass == writeAsPartial)
                return;

            WriteAsPartialClass = writeAsPartial;
            Save();
        }

        public void SetUseBaseClassForItems(bool useBaseClass)
        {
            if (UseBaseClassForItems == useBaseClass)
                return;

            UseBaseClassForItems = useBaseClass;
            Save();
        }

        public void SetParentFolderPath(string assetPath)
        {
            if (string.Equals(ParentFolderPath, assetPath, StringComparison.Ordinal))
                return;

            ParentFolderPath = assetPath;
            Save();
        }
    }
}
