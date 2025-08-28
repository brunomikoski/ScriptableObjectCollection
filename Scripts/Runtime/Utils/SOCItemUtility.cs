using System;
using System.IO;
#if  UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class SOCItemUtility
    {
        public static void MoveItem(ScriptableObject item, ScriptableObjectCollection targetCollection,
            Action onCompleteCallback = null)
        {
            if (item is ISOCItem iItem)
            {
                MoveItem(iItem, targetCollection, onCompleteCallback);
            }
        }

        public static string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                       .ToUpperInvariant();
        }

        public static void MoveItem(ISOCItem item, ScriptableObjectCollection targetCollection, Action onCompleteCallback = null)
        {
#if  UNITY_EDITOR
            Undo.RecordObject(item.Collection, "Move Item");
            Undo.RecordObject(targetCollection, "Move Item");

            item.Collection.Remove(item);
            targetCollection.Add(item);
            item.SetCollection(targetCollection);

            string itemPath = AssetDatabase.GetAssetPath(item as ScriptableObject);
            string targetCollectionPath = AssetDatabase.GetAssetPath(targetCollection);

            if (!string.IsNullOrEmpty(itemPath) && !string.IsNullOrEmpty(targetCollectionPath))
            {
                string directory = Path.GetDirectoryName(targetCollectionPath);

                string itemsFolderPath = Path.Combine(directory, "Items");
                bool hasItemsFolder = AssetDatabase.IsValidFolder(itemsFolderPath);

                string finalDirectory = hasItemsFolder ? itemsFolderPath : directory;
                string fileName = Path.GetFileName(itemPath);
                string newPathCandidate = Path.Combine(finalDirectory, fileName);

                if (NormalizePath(itemPath) != NormalizePath(newPathCandidate))
                {
                    string newPath = AssetDatabase.GenerateUniqueAssetPath(newPathCandidate);
                    AssetDatabase.MoveAsset(itemPath, newPath);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            onCompleteCallback?.Invoke();
#endif
        }
    }
}