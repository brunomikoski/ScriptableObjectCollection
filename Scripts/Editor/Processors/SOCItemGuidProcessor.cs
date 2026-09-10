using System;
using System.Collections.Generic;
using UnityEditor;

namespace BrunoMikoski.ScriptableObjectCollections
{
    internal sealed class SOCItemGuidProcessor : AssetPostprocessor
    {
        // LongGuid -> Unity asset GUID (.meta GUID) of the asset that owns it.
        // Asset GUIDs survive renames and moves while duplicated assets always receive
        // a new one, so they are a rename-safe identity for duplicate detection.
        private static readonly Dictionary<LongGuid, string> AssetGuidByItemGuid = new Dictionary<LongGuid, string>();
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
            AssetGuidByItemGuid.Clear();
            string[] assetGuids = AssetDatabase.FindAssets($"t:{nameof(ScriptableObjectCollectionItem)}");
            for (int i = 0; i < assetGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                ScriptableObjectCollectionItem item = AssetDatabase.LoadAssetAtPath<ScriptableObjectCollectionItem>(path);
                if (item == null)
                    continue;

                LongGuid guid = item.GUID;
                if (!guid.IsValid())
                    continue;

                AssetGuidByItemGuid.TryAdd(guid, assetGuids[i]);
            }

            indexInitialized = true;
        }

        private static bool TryGetOwnerAssetGuid(LongGuid itemGuid, out string ownerAssetGuid)
        {
            if (!indexInitialized)
                RebuildIndex();
            return AssetGuidByItemGuid.TryGetValue(itemGuid, out ownerAssetGuid);
        }

        private static void UpsertIndex(ScriptableObjectCollectionItem item, string assetGuid)
        {
            if (string.IsNullOrEmpty(assetGuid))
                return;
            if (!indexInitialized)
                RebuildIndex();
            LongGuid guid = item.GUID;
            if (!guid.IsValid())
                return;
            AssetGuidByItemGuid[guid] = assetGuid;
        }

        private static void RemoveFromIndexByDeletedPath(string deletedPath)
        {
            if (!indexInitialized)
                return;

            string deletedAssetGuid = AssetDatabase.AssetPathToGUID(deletedPath);
            if (string.IsNullOrEmpty(deletedAssetGuid))
                return;

            List<LongGuid> keysToRemove = null;
            foreach (KeyValuePair<LongGuid, string> kvp in AssetGuidByItemGuid)
            {
                if (string.Equals(kvp.Value, deletedAssetGuid, StringComparison.Ordinal))
                {
                    keysToRemove ??= new List<LongGuid>();
                    keysToRemove.Add(kvp.Key);
                }
            }

            if (keysToRemove == null)
                return;

            for (int i = 0; i < keysToRemove.Count; i++)
                AssetGuidByItemGuid.Remove(keysToRemove[i]);
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
                RemoveFromIndexByDeletedPath(del);
            }

            foreach (string path in importedAssets)
            {
                ScriptableObjectCollectionItem item = AssetDatabase.LoadAssetAtPath<ScriptableObjectCollectionItem>(path);
                if (item == null)
                    continue;

                string assetGuid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(assetGuid))
                    continue;

                bool changed = EnsureValidAndUniqueGuid(item, assetGuid);
                if (changed)
                {
                    EditorUtility.SetDirty(item);
                    anyDirty = true;
                }

                UpsertIndex(item, assetGuid);

                ScriptableObjectCollection collection = item.Collection;
                if (collection != null && collection.Add(item))
                    anyDirty = true;
            }

            for (int i = 0; i < movedAssets.Length; i++)
            {
                string newPath = movedAssets[i];
                ScriptableObjectCollectionItem item = AssetDatabase.LoadAssetAtPath<ScriptableObjectCollectionItem>(newPath);
                if (item == null)
                    continue;

                // Moves and renames keep the same asset GUID; this only heals missing index entries.
                UpsertIndex(item, AssetDatabase.AssetPathToGUID(newPath));
            }

            if (anyDirty)
            {
                AssetDatabase.SaveAssets();
            }
        }

        private static bool EnsureValidAndUniqueGuid(ScriptableObjectCollectionItem item, string assetGuid)
        {
            LongGuid itemGuid = item.GUID;

            if (!itemGuid.IsValid())
            {
                item.GenerateNewGUID();
                return true;
            }

            if (!TryGetOwnerAssetGuid(itemGuid, out string ownerAssetGuid))
                return false;

            if (string.Equals(ownerAssetGuid, assetGuid, StringComparison.Ordinal))
                return false;

            // A stale index entry must never cost an item its guid; only an existing
            // asset that still holds this guid counts as a real duplicate.
            if (!IsCurrentOwner(ownerAssetGuid, itemGuid))
                return false;

            item.GenerateNewGUID();
            return true;
        }

        private static bool IsCurrentOwner(string ownerAssetGuid, LongGuid itemGuid)
        {
            string ownerPath = AssetDatabase.GUIDToAssetPath(ownerAssetGuid);
            if (string.IsNullOrEmpty(ownerPath))
                return false;

            ScriptableObjectCollectionItem owner = AssetDatabase.LoadAssetAtPath<ScriptableObjectCollectionItem>(ownerPath);
            if (owner == null)
                return false;

            return owner.GUID == itemGuid;
        }
    }
}
