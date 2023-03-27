using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class ScriptableObjectCollectionUtils
    {
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
    }
}
