using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Utilities for dealing with scripts.
    /// </summary>
    public static class ScriptUtility 
    {
        public static bool TryGetFolderOfClass(Type classType, out string folderPath)
        {
            folderPath = null;

            if (!TryGetScriptOfClass(classType, out MonoScript script))
                return false;

            string scriptPath = AssetDatabase.GetAssetPath(script);
            if (string.IsNullOrEmpty(scriptPath))
                return false;

            folderPath = Path.GetDirectoryName(scriptPath);
            return true;
        }

        public static bool TryGetScriptOfClass(Type classType, out MonoScript script)
        {
            string[] scriptGuids = AssetDatabase.FindAssets($"t:script {classType.Name}");
            if (scriptGuids.Length == 0)
            {
                Debug.LogWarning($"Could not find corresponding script for class '{classType.Name}'. " +
                                 $"Check that the script file name matches the class name.");
                script = null;
                return false;
            }

            foreach (string guid in scriptGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript candidate = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (candidate != null && candidate.GetClass() == classType)
                {
                    script = candidate;
                    return true;
                }
            }

            script = null;
            return false;
        }
    }
}
