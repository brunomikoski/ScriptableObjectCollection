using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public sealed class CollectionsAssetsPostProcessor : AssetPostprocessor
    {
        private const string REFRESH_REGISTRY_AFTER_RECOMPILATION_KEY = "RefreshRegistryAfterRecompilationKey";

        private static bool RefreshRegistryAfterRecompilation
        {
            get => EditorPrefs.GetBool(REFRESH_REGISTRY_AFTER_RECOMPILATION_KEY, false);
            set => EditorPrefs.SetBool(REFRESH_REGISTRY_AFTER_RECOMPILATION_KEY, value);
        }
        
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            for (int i = 0; i < importedAssets.Length; i++)
            {
                string importedAssetPath = importedAssets[i];

                Type type = AssetDatabase.GetMainAssetTypeAtPath(importedAssetPath);

                if (typeof(ScriptableObject).IsAssignableFrom(type))
                {
                    ScriptableObject collectionItem =
                        AssetDatabase.LoadAssetAtPath<ScriptableObject>(importedAssetPath);

                    if (collectionItem != null)
                    {
                        if (collectionItem is ISOCItem socItem)
                        {
                            if (socItem.Collection == null)
                            {
                                Debug.LogError(
                                    $"CollectionItem ({collectionItem.name}) has null Collection, please assign it some Collection",
                                    collectionItem
                                );
                            }
                            else
                            {
                                if (!socItem.Collection.Contains(collectionItem))
                                {
                                    if (socItem.Collection.TryGetItemByGUID(socItem.GUID, out _))
                                        socItem.GenerateNewGUID();
                                    
                                    socItem.Collection.Add(collectionItem);
                                    Debug.Log($"{collectionItem.name} has collection assigned "
                                              + $"{socItem.Collection} but its missing from collection list, adding it");
                                }
                            }
                        }
                    }
                }
                
                if (typeof(ScriptableObjectCollection).IsAssignableFrom(type))
                {
                    ScriptableObjectCollection collection =
                        AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(importedAssetPath);

                    if (collection == null)
                        continue;

                    if (!CollectionsRegistry.Instance.IsKnowCollection(collection))
                    {
                        RefreshRegistry();
                        Debug.Log($"New collection found on the Project {collection.name}, refreshing the Registry");
                        return;
                    }
                }
            }
        }

        private static void RefreshRegistry()
        {
            if (EditorApplication.isCompiling)
            {
                RefreshRegistryAfterRecompilation = true;
                return;
            }

            EditorApplication.delayCall += () => { CollectionsRegistry.Instance.ReloadCollections(); };
        }


        [DidReloadScripts]
        static void OnAfterScriptsReloading()
        {
            if (RefreshRegistryAfterRecompilation)
                RefreshRegistry();

            RefreshRegistryAfterRecompilation = false;
        }
    }
}
