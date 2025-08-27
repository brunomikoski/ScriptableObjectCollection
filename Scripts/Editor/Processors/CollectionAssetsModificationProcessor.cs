using System;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

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
            
            if (type.IsSubclassOf(typeof(ScriptableObject)))
            {
                ScriptableObject collectionItem =
                    AssetDatabase.LoadAssetAtPath<ScriptableObject>(targetAssetPath);

                ISOCItem socItem = collectionItem as ISOCItem;
                if (socItem == null)
                    return AssetDeleteResult.DidNotDelete;

                if (socItem.Collection == null)
                    return AssetDeleteResult.DidNotDelete;

                socItem.Collection.Remove(collectionItem);
                return AssetDeleteResult.DidNotDelete;
            }

            if (type.IsSubclassOf(typeof(ScriptableObjectCollection)) || type == typeof(ScriptableObjectCollection))
            {
                ScriptableObjectCollection collection =
                    AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(targetAssetPath);

                CollectionsRegistry.Instance.UnregisterCollection(collection);

                return AssetDeleteResult.DidNotDelete;
            }
            
            return AssetDeleteResult.DidNotDelete;
        }
        
    }
}