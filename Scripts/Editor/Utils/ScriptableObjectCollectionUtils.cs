using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class ScriptableObjectCollectionUtils
    {
        public static T CreateScriptableObjectOfType<T>(DefaultAsset parentFolder, bool createFolderForThisCollection,
            string targetName) where T : ScriptableObject
        {
            return CreateScriptableObjectOfType<T>(AssetDatabase.GetAssetPath(parentFolder),
                createFolderForThisCollection, targetName);
        }

        public static T CreateScriptableObjectOfType<T>(string parentFolderPath, bool createFolderForThisCollection,
            string targetName) where T : ScriptableObject
        {
            return CreateScriptableObjectOfType(typeof(T), parentFolderPath, createFolderForThisCollection, targetName) as T;
        }

        public static ScriptableObject CreateScriptableObjectOfType(Type targetType, string parentFolderPath,
            bool createFolderForThisCollection, string targetName)
        {
            ScriptableObject targetCollection = ScriptableObject.CreateInstance(targetType);
            targetCollection.name = targetName;

            string targetFolderPath = parentFolderPath;
            if (createFolderForThisCollection)
                targetFolderPath = Path.Combine(targetFolderPath, $"{targetName}");

            AssetDatabaseUtils.CreatePathIfDontExist(Path.Combine(targetFolderPath, "Items"));

            string collectionAssetPath = Path.Combine(targetFolderPath, $"{targetName}.asset");
            AssetDatabase.CreateAsset(targetCollection, collectionAssetPath);
            return targetCollection;
        }

        public static ScriptableObject CreateScriptableObjectOfType(Type targetType, DefaultAsset parentFolder, bool createFoldForThisCollection, string collectionName)
        {
            return CreateScriptableObjectOfType(targetType, AssetDatabase.GetAssetPath(parentFolder),
                createFoldForThisCollection, collectionName);
        }
    }
}
