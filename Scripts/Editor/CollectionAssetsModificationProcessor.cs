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
            
            if (type.IsSubclassOf(typeof(CollectableScriptableObject)))
            {
                CollectableScriptableObject collectable =
                    AssetDatabase.LoadAssetAtPath<CollectableScriptableObject>(targetAssetPath);

                collectable.Collection.Remove(collectable);
                return AssetDeleteResult.DidNotDelete;
            }
            
            if (type.IsSubclassOf(typeof(ScriptableObjectCollection)))
            {
                ScriptableObjectCollection collection =
                    AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(targetAssetPath);

                CollectionsRegistry.Instance.DeleteCollection(collection);
                return AssetDeleteResult.DidNotDelete;
            }
            
            return AssetDeleteResult.DidNotDelete;
        }
        
    }
}