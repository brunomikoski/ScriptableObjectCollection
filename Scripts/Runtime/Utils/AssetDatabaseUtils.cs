using System.IO;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class AssetDatabaseUtils
    {
        public static void CreatePathIfDontExist(string targetPath)
        {
#if UNITY_EDITOR
            targetPath = PathUtils.FixPathForPlatform(targetPath);
            char separator = PathUtils.GetPlatformSeparator();
            
            string[] pathSegments = targetPath.Split(separator);

            string cumulativePath = "Assets";
            bool shouldRefresh = false;
            for (int i = 0; i < pathSegments.Length; i++)
            {
                string pathSegment = pathSegments[i];
                if (string.IsNullOrEmpty(pathSegment))
                    continue;

                if (string.Equals(pathSegment, cumulativePath))
                    continue;

                if (!UnityEditor.AssetDatabase.IsValidFolder($"{cumulativePath}{separator}{pathSegment}"))
                {
                    UnityEditor.AssetDatabase.CreateFolder(cumulativePath, pathSegment);
                    shouldRefresh = true;
                }

                cumulativePath += $"{separator}{pathSegment}";
            }

            if (shouldRefresh)
            {
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }
#endif
        }

        public static void RenameAsset(Object targetObject, string newName)
        {
#if UNITY_EDITOR
            targetObject.name = newName;
            UnityEditor.AssetDatabase.RenameAsset(UnityEditor.AssetDatabase.GetAssetPath(targetObject), newName);
            ObjectUtility.SetDirty(targetObject);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
    }
}
