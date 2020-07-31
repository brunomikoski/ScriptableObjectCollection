using UnityEditor;
using UnityEngine;

namespace System.IO
{
    public static class PathUtility
    {
        public static string GetRelativePath(string path)
        {
            Uri absolutePath = new Uri(path);
            Uri relativePath = new Uri(Application.dataPath);
            return $"../{relativePath.MakeRelativeUri(absolutePath)}";
        }

        public static bool IsObjectDeeperThanObject(Object childObject, Object parentObject)
        {
            Uri childPath = new Uri(Path.GetFullPath(AssetDatabase.GetAssetPath((UnityEngine.Object) childObject)));
            Uri parentPath = new Uri(Path.GetFullPath(AssetDatabase.GetAssetPath((UnityEngine.Object) parentObject)));
            return parentPath.IsBaseOf(childPath);
        }
    }
}
