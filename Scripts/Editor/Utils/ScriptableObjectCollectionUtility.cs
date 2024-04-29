using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class ScriptableObjectCollectionUtility
    {
        private const string COLLECTION_CUSTOM_EDITOR_GO_TO_ITEM_INDEX_KEY = "COLLECTION_CUSTOM_EDITOR_GO_TO_ITEM_INDEX";

        public static T CreateScriptableObjectOfType<T>(DefaultAsset parentFolder, string name) where T : ScriptableObject
        {
            return CreateScriptableObjectOfType(typeof(T), AssetDatabase.GetAssetPath(parentFolder), name) as T;
        }
        
        public static T CreateScriptableObjectOfType<T>(string path, string name) where T : ScriptableObject
        {
            return CreateScriptableObjectOfType(typeof(T), path, name) as T;
        }
        
        [Obsolete("Please use the overload that contains a complete path.")]
        public static T CreateScriptableObjectOfType<T>(DefaultAsset parentFolder, bool createFolderForThisCollection,
            string targetName) where T : ScriptableObject
        {
            return CreateScriptableObjectOfType<T>(AssetDatabase.GetAssetPath(parentFolder),
                createFolderForThisCollection, targetName);
        }

        [Obsolete("Please use the overload that contains a complete path.")]
        public static T CreateScriptableObjectOfType<T>(string parentFolderPath, bool createFolderForThisCollection,
            string targetName) where T : ScriptableObject
        {
            if (createFolderForThisCollection)
                parentFolderPath = Path.Combine(parentFolderPath, $"{targetName}");
            
            return CreateScriptableObjectOfType<T>(parentFolderPath, targetName);
        }
        
        public static ScriptableObject CreateScriptableObjectOfType(Type targetType, string path, string name)
        {
            ScriptableObject targetCollection = ScriptableObject.CreateInstance(targetType);
            targetCollection.name = name;
            
            AssetDatabaseUtils.CreatePathIfDoesntExist(Path.Combine(path, "Items"));

            string collectionAssetPath = Path.Combine(path, $"{name}.asset");
            AssetDatabase.CreateAsset(targetCollection, collectionAssetPath);
            return targetCollection;
        }

        [Obsolete("Please use the overload that contains a complete path.")]
        public static ScriptableObject CreateScriptableObjectOfType(Type targetType, string parentFolderPath,
            bool createFolderForThisCollection, string targetName)
        {
            if (createFolderForThisCollection)
                parentFolderPath = Path.Combine(parentFolderPath, $"{targetName}");
            
            return CreateScriptableObjectOfType(targetType, parentFolderPath, targetName);
        }

        [Obsolete("Please use the overload that contains a complete path.")]
        public static ScriptableObject CreateScriptableObjectOfType(
            Type targetType, DefaultAsset parentFolder, bool createFoldForThisCollection, string collectionName)
        {
            return CreateScriptableObjectOfType(
                targetType, AssetDatabase.GetAssetPath(parentFolder),
                createFoldForThisCollection, collectionName);
        }

        public static void GoToItem(ISOCItem socItem)
        {
            SessionState.SetInt(COLLECTION_CUSTOM_EDITOR_GO_TO_ITEM_INDEX_KEY, socItem.Collection.IndexOf(socItem));
            Selection.activeObject = socItem.Collection;
        }

        public static bool IsTryingToGoToItem(out int targetIndex)
        {
            targetIndex = SessionState.GetInt(COLLECTION_CUSTOM_EDITOR_GO_TO_ITEM_INDEX_KEY, -1);
            return targetIndex != -1;
        }

        public static void ClearGoToItem()
        {
            SessionState.EraseInt(COLLECTION_CUSTOM_EDITOR_GO_TO_ITEM_INDEX_KEY);
        }
    }
}
