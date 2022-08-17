using System.IO;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class AssetDatabaseUtils
    {
        public static void CreatePathIfDontExist(string targetPath)
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
            UnityEditor.AssetDatabase.RenameAsset(UnityEditor.AssetDatabase.GetAssetPath(targetObject), newName);
            targetObject.name = newName;
            ObjectUtility.SetDirty(targetObject);
#endif
        }
    }
}
