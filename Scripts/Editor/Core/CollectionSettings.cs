using System;
using System.IO;
using UnityEditor;

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

        public CollectionSettings(ScriptableObjectCollection targetCollection)
        {
            Guid = targetCollection.GUID;
            string targetNamespace = targetCollection.GetItemType().Namespace;
            if (!string.IsNullOrEmpty(SOCSettings.Instance.NamespacePrefix))
                targetNamespace = $"{SOCSettings.Instance.NamespacePrefix}";
            
            Namespace = targetNamespace;

            if (!string.IsNullOrEmpty(SOCSettings.Instance.generatedScriptsDefaultFilePath))
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
        }
    }
}