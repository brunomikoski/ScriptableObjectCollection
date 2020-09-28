using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class CollectionUtility
    {
        private static Dictionary<Object, Editor> itemToEditor =
            new Dictionary<Object, Editor>();

        private static Dictionary<Object, bool> objectToFoldOut = new Dictionary<Object, bool>();

        [MenuItem("Assets/Create/ScriptableObject Collection/New Collection", false, 100)]
        private static void CreateNewItem()
        {
            string targetPath = "";
            if (Selection.objects.FirstOrDefault() is DefaultAsset folder)
                targetPath = AssetDatabase.GetAssetPath(folder);
            
            CreateCollectionWizzard.Show(targetPath);
        }
        
        [MenuItem("Assets/Create/ScriptableObject Collection/Create Settings", false, 200)]
        private static void CreateSettings()
        {
            ScriptableObjectCollectionSettings.LoadOrCreateInstance<ScriptableObjectCollection>();
        }
        
        public static Editor GetOrCreateEditorForItem(Object collectionItem)
        {
            if (itemToEditor.TryGetValue(collectionItem, out Editor customEditor))
                return customEditor;
            
            Editor.CreateCachedEditor(collectionItem, null, ref customEditor);
            itemToEditor.Add(collectionItem, customEditor);
            return customEditor;
        }
        
        public static bool IsFoldoutOpen(Object targetObject)
        {
            if (targetObject.IsNull())
                return false;
            
            bool value;
            if(!objectToFoldOut.TryGetValue(targetObject, out value))
                objectToFoldOut.Add(targetObject, value);

            return value;
        }

        public static void SetFoldoutOpen(Object targetObject, bool value)
        {
            if (!objectToFoldOut.ContainsKey(targetObject))
                objectToFoldOut.Add(targetObject, value);
            else
                objectToFoldOut[targetObject] = value;
        }

       
    }
}

