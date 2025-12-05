using System;
using System.Collections.Generic;
using UnityEditor;

namespace BrunoMikoski.ScriptableObjectCollections
{
    internal sealed class SOCItemGuidProcessor : AssetPostprocessor
    {
        private static readonly Dictionary<LongGuid, string> PathByGuid = new Dictionary<LongGuid, string>();
        private static bool indexInitialized;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            RebuildIndex();
            EditorApplication.projectChanged += OnProjectChanged;
        }

        private static void OnProjectChanged()
        {
            RebuildIndex();
        }

        private static void RebuildIndex()
        {
            PathByGuid.Clear();
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(ScriptableObjectCollectionItem)}");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                ScriptableObjectCollectionItem item = AssetDatabase.LoadAssetAtPath<ScriptableObjectCollectionItem>(path);
                if (item == null)
                    continue;

                LongGuid guid = item.GUID;
                if (!guid.IsValid())
                    continue;

                PathByGuid.TryAdd(guid, path);
            }

            indexInitialized = true;
        }

        private static bool TryGetOwner(LongGuid guid, out string path)
        {
            if (!indexInitialized)
                RebuildIndex();
            return PathByGuid.TryGetValue(guid, out path);
        }

        private static void UpsertIndex(ScriptableObjectCollectionItem item, string path)
        {
            if (!indexInitialized)
                RebuildIndex();
            LongGuid guid = item.GUID;
            if (!guid.IsValid())
                return;
            PathByGuid[guid] = path;
        }

        private static void RemoveFromIndex(ScriptableObjectCollectionItem item)
        {
            if (!indexInitialized)
                return;
            LongGuid guid = item.GUID;
            if (!guid.IsValid())
                return;
            
            if (PathByGuid.TryGetValue(guid, out string existing) && existing == AssetDatabase.GetAssetPath(item))
                PathByGuid.Remove(guid);
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool anyDirty = false;

            foreach (string del in deletedAssets)
            {
                ScriptableObjectCollectionItem item = AssetDatabase.LoadAssetAtPath<ScriptableObjectCollectionItem>(del);
                if (item != null)
                    RemoveFromIndex(item);
            }

            foreach (string path in importedAssets)
            {
                ScriptableObjectCollectionItem item = AssetDatabase.LoadAssetAtPath<ScriptableObjectCollectionItem>(path);
                if (item == null)
                    continue;

                bool changed = EnsureValidAndUniqueGuid(item, path);
                if (changed)
                {
                    EditorUtility.SetDirty(item);
                    anyDirty = true;
                }

                UpsertIndex(item, path);
            }

            for (int i = 0; i < movedAssets.Length; i++)
            {
                string newPath = movedAssets[i];
                ScriptableObjectCollectionItem item = AssetDatabase.LoadAssetAtPath<ScriptableObjectCollectionItem>(newPath);
                if (item == null)
                    continue;

                UpsertIndex(item, newPath);
            }

            if (anyDirty)
            {
                AssetDatabase.SaveAssets();
            }
        }

        private static bool EnsureValidAndUniqueGuid(ScriptableObjectCollectionItem item, string path)
        {
            LongGuid guid = item.GUID;

            if (!guid.IsValid())
            {
                item.GenerateNewGUID();
                return true;
            }

            if (TryGetOwner(guid, out string ownerPath) && !string.Equals(ownerPath, path, StringComparison.Ordinal))
            {
                item.GenerateNewGUID();
                return true;
            }

            return false;
        }
    }
}
