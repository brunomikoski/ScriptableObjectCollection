using UnityEditor;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public class CollectionAssetsModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        public static AssetDeleteResult OnWillDeleteAsset(string targetAssetPath, RemoveAssetOptions removeAssetOptions)
        {
            if (AssetDatabase.LoadMainAssetAtPath(targetAssetPath).GetType()
                .IsSubclassOf(typeof(CollectableScriptableObject)))
            {
                CollectableScriptableObject collectable =
                    AssetDatabase.LoadAssetAtPath<CollectableScriptableObject>(targetAssetPath);

                collectable.Collection.Remove(collectable);
                return AssetDeleteResult.DidNotDelete;
            }
            
            if (AssetDatabase.LoadMainAssetAtPath(targetAssetPath).GetType()
                .IsSubclassOf(typeof(ScriptableObjectCollection)))
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