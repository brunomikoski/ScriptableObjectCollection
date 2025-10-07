using System.IO;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class AssetDatabaseUtils
    {
        public static void CreatePathIfDoesntExist(string targetPath)
        {
#if UNITY_EDITOR
            string absolutePath = Path.GetFullPath(targetPath);

            if (Directory.Exists(absolutePath))
                return;

            Directory.CreateDirectory(absolutePath);
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public static void RenameAsset(Object targetObject, string newName)
        {
#if UNITY_EDITOR
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(targetObject);

            string guid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
            UnityEditor.AssetDatabase.RenameAsset(assetPath, newName);
            ObjectUtility.SetDirty(targetObject);
            UnityEditor.AssetDatabase.ImportAsset(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
#endif
        }
    }
}
