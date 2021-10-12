using System.Collections.Generic;
using System.IO;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class NamespaceUtility
    {
        public const char Separator = '.';
        
        /// <summary>
        /// First directory is the Assets folder so we can skip that one.
        /// </summary>
        private const int FoldersToAlwaysSkipCount = 1;

        private static readonly List<string> FoldersToIgnore = new List<string> { "Scripts" };

        private static readonly string[] CharactersToRemove =
        {
            "[", "]", " ", "_", "-", "/", "\\",
        };

        private static string GetNamespaceForDirectoryName(string name)
        {
            for (int i = 0; i < CharactersToRemove.Length; i++)
            {
                name = name.Replace(CharactersToRemove[i], string.Empty);
            }
            return name;
        }

        public static string GetNamespaceForPath(string path, int depth = 2)
        {
            path = path.ToPathWithConsistentSeparators();
            string[] folders = path.Split(Path.AltDirectorySeparatorChar);

            string @namespace = "";
            int namespacesAddedCount = 0;
            for (int i = FoldersToAlwaysSkipCount; i < folders.Length; i++)
            {
                if (FoldersToIgnore.Contains(folders[i]))
                    continue;
                
                if (namespacesAddedCount != 0)
                    @namespace += Separator;
                
                @namespace += GetNamespaceForDirectoryName(folders[i]);
                namespacesAddedCount++;

                if (namespacesAddedCount > depth)
                    return @namespace;
            }
            
            return @namespace;
        }
    }
}
