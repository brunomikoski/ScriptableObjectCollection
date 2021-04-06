using System;
using UnityEditor;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public class CollectionAssetsModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        public static AssetDeleteResult OnWillDeleteAsset(string targetAssetPath, RemoveAssetOptions removeAssetOptions)
        {
            Object mainAssetAtPath = AssetDatabase.LoadMainAssetAtPath(targetAssetPath);
            if (mainAssetAtPath == null)
                return AssetDeleteResult.DidNotDelete;
            
            Type type = mainAssetAtPath.GetType();
            
            if (type.IsSubclassOf(typeof(ScriptableObjectCollectionItem)))
            {
                ScriptableObjectCollectionItem collectionItem =
                    AssetDatabase.LoadAssetAtPath<ScriptableObjectCollectionItem>(targetAssetPath);

                collectionItem.Collection.Remove(collectionItem);
                return AssetDeleteResult.DidNotDelete;
            }
            
            return AssetDeleteResult.DidNotDelete;
        }
        
    }
}
