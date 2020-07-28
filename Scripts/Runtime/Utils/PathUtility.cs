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
    }
}
