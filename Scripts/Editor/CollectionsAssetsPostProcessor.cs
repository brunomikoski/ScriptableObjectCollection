using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public sealed class CollectionsAssetsPostProcessor : AssetPostprocessor
    {
        private const string REFRESH_REGISTRY_AFTER_RECOMPILATION_KEY = "RefreshRegistryAfterRecompilationKey";

        private static Dictionary<Type, ScriptableObjectCollection> cachedTypeToCollections;
        private static Dictionary<Type, ScriptableObjectCollection> typeToCollections
        {
            get
            {
                if (cachedTypeToCollections == null)
                {
                    cachedTypeToCollections = new Dictionary<Type, ScriptableObjectCollection>();
                    string[] collectionGUIDS = AssetDatabase.FindAssets($"t:{(nameof(ScriptableObjectCollection))}");

                    foreach (string guid in collectionGUIDS)
                    {
                        ScriptableObjectCollection collection =
                            AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(
                                AssetDatabase.GUIDToAssetPath(guid));
                        if(collection == null)
                            continue;

                        cachedTypeToCollections.Add(collection.GetCollectionType(), collection);
                    }
                }

                return cachedTypeToCollections;
            }
        }
        
        private static bool RefreshRegistryAfterRecompilation
        {
            get => EditorPrefs.GetBool(REFRESH_REGISTRY_AFTER_RECOMPILATION_KEY, false);
            set => EditorPrefs.SetBool(REFRESH_REGISTRY_AFTER_RECOMPILATION_KEY, value);
        }
        
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool shouldRefreshRegistry = false;
            for (int i = 0; i < importedAssets.Length; i++)
            {
                string importedAssetPath = importedAssets[i];

                Type type = AssetDatabase.GetMainAssetTypeAtPath(importedAssetPath);

                if (typeof(CollectableScriptableObject).IsAssignableFrom(type))
                {
                    CollectableScriptableObject collectableScriptableObject =
                        AssetDatabase.LoadAssetAtPath<CollectableScriptableObject>(importedAssetPath);

                    if (collectableScriptableObject != null)
                    {
                        if (collectableScriptableObject.Collection == null)
                        {
                            if(typeToCollections.TryGetValue(type, out ScriptableObjectCollection collection))
                            {
                                collection.Add(collectableScriptableObject);
                            }
                        }
                        else
                        {
                            if (!collectableScriptableObject.Collection.Contains(collectableScriptableObject))
                                collectableScriptableObject.Collection.Add(collectableScriptableObject);
                            
                            collectableScriptableObject.Collection.ValidateGUID();
                        }
                    }
                }
                
                if (typeof(ScriptableObjectCollection).IsAssignableFrom(type))
                {
                    ScriptableObjectCollection collection =
                        AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(importedAssetPath);

                    if (collection == null)
                        continue;
                    
                    if (!CollectionsRegistry.Instance.IsKnowCollectionGUID(collection.GUID))
                        shouldRefreshRegistry = true;
                }
            }

            for (int i = 0; i < deletedAssets.Length; i++)
            {
                string deletedAsset = deletedAssets[i];
                string guid = AssetDatabase.AssetPathToGUID(deletedAsset);
                if (CollectionsRegistry.Instance.IsKnowCollectionGUID(guid))
                {
                    RefreshRegistry();
                    return;
                }
            }
            
            for (int i = 0; i < movedFromAssetPaths.Length; i++)
            {
                string movedAsset = movedAssets[i];
                string guid = AssetDatabase.AssetPathToGUID(movedAsset);
                if (CollectionsRegistry.Instance.IsKnowCollectionGUID(guid))
                {
                    RefreshRegistry();
                    return;
                }
            }

            if (shouldRefreshRegistry)
                RefreshRegistry();
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
