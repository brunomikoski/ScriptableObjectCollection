using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class ScriptableObjectCollectionUtils
    {
        public static T CreateScriptableObjectOfType<T>(DefaultAsset parentFolder, bool createFoldForThisCollection,
            string targetName) where T : ScriptableObject
        {
            return CreateScriptableObjectOfType<T>(AssetDatabase.GetAssetPath(parentFolder),
                createFoldForThisCollection, targetName);
        }

        public static T CreateScriptableObjectOfType<T>(string parentFolderPath, bool createFoldForThisCollection,
            string targetName) where T : ScriptableObject
        {
            T targetCollection = ScriptableObject.CreateInstance<T>();
            targetCollection.name = targetName;

            string targetFolderPath = parentFolderPath;
            if (createFoldForThisCollection)
                targetFolderPath = Path.Combine(targetFolderPath, $"{targetName}");

            AssetDatabaseUtils.CreatePathIfDontExist(Path.Combine(targetFolderPath, "Items"));

            string collectionAssetPath = Path.Combine(targetFolderPath, $"{targetName}.asset");
            AssetDatabase.CreateAsset(targetCollection, collectionAssetPath);
            return targetCollection;
        }
    }
}
