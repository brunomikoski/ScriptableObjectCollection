using System;
using BrunoMikoski.ScriptableObjectCollections.Core;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public class ScriptableObjectCollectionSettings : ResourceScriptableObjectSingleton<ScriptableObjectCollectionSettings>
    {
#if UNITY_EDITOR
        
        [SerializeField]
        private UnityEditor.DefaultAsset collectionAssetsFolder;
        
        [SerializeField]
        private UnityEditor.DefaultAsset collectionScriptsFolder;
        
        [SerializeField]
        private UnityEditor.DefaultAsset staticScriptsFolder;

#endif
        [SerializeField]
        private string nameSpace;

        private void OnEnable()
        {
#if UNITY_EDITOR
            staticScriptsFolder =
                UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.DefaultAsset>(CollectionUtility.StaticGeneratedScriptsFolderPath);
            
            collectionAssetsFolder =
                UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.DefaultAsset>(CollectionUtility.ScriptableObjectFolderPath);
            
            collectionScriptsFolder =
                UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.DefaultAsset>(CollectionUtility.ScriptsFolderPath);

            nameSpace = CollectionUtility.TargetNamespace;
#endif
        }

        private void OnValidate()
        {
#if UNITY_EDITOR            
            CollectionUtility.StaticGeneratedScriptsFolderPath = UnityEditor.AssetDatabase.GetAssetPath(staticScriptsFolder);
            CollectionUtility.ScriptableObjectFolderPath = UnityEditor.AssetDatabase.GetAssetPath(collectionAssetsFolder);
            CollectionUtility.ScriptsFolderPath = UnityEditor.AssetDatabase.GetAssetPath(collectionScriptsFolder);
            CollectionUtility.TargetNamespace = nameSpace;
#endif
        }
    }
}
