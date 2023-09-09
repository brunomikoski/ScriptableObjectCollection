using System;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Utilities for dealing with scripts.
    /// </summary>
    public static class ScriptUtility 
    {
        public static bool TryGetScriptOfClass(Type classType, out MonoScript script)
        {
            string[] scriptGuids = AssetDatabase.FindAssets($"t:script {classType.Name}");
            if (scriptGuids.Length == 0)
            {
                Debug.LogWarning($"Could not find corresponding script for class '{classType.Name}'. " +
                                 $"Check that the script is called '{classType.Name}'.");
                script = null;
                return false;
            }

            string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuids[0]);
            script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            return true;
        }
    }
}
