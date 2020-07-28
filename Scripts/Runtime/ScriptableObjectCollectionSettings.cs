using BrunoMikoski.ScriptableObjectCollections.Core;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public class ScriptableObjectCollectionSettings : ResourceScriptableObjectSingleton<ScriptableObjectCollectionSettings>
    {
#if UNITY_EDITOR
        
        [SerializeField]
        private UnityEditor.DefaultAsset collectionAssetsFolder;
        public string CollectionAssetsFolderPath => UnityEditor.AssetDatabase.GetAssetPath(collectionAssetsFolder);

        [SerializeField]
        private UnityEditor.DefaultAsset collectionScriptsFolder;
        public string CollectionScriptsFolderPath => UnityEditor.AssetDatabase.GetAssetPath(collectionScriptsFolder);
        
        [SerializeField]
        private UnityEditor.DefaultAsset staticScriptsFolder;
        public string StaticScriptsFolderPath => UnityEditor.AssetDatabase.GetAssetPath(staticScriptsFolder);

#endif
        [SerializeField]
        private string nameSpace;
        public string NameSpace => nameSpace;
    }
}
