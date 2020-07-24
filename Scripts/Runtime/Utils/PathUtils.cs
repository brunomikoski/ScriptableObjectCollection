using UnityEngine;

namespace System.IO
{
    public static class PathUtils
    {
        public static string AbsoluteToRelativePath(string absoluteFilePath)
        {
            return "Assets" + absoluteFilePath.Substring(Application.dataPath.Length);
        }

        public static string RelativeToAbsolutePath(string relativeFilePath)
        {
            return Path.GetFullPath(relativeFilePath);
        }

        public static string FixPathForPlatform(string path)
        {
            path = path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (Application.platform == RuntimePlatform.WindowsEditor)
                path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            return path;
        }

        public static char GetPlatformSeparator()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
                return Path.DirectorySeparatorChar;

            return Path.AltDirectorySeparatorChar;
        }
        
    }
}
