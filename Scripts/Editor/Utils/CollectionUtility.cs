using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class CollectionUtility
    {
        

        private static Dictionary<int, bool> objectToFoldOut = new Dictionary<int, bool>();

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
            ScriptableObjectCollectionSettings.LoadOrCreateInstance();
        }

        [MenuItem("Assets/Create/ScriptableObject Collection/Create Settings", true, 200)]
        private static bool CreateSettings_Validation()
        {
            return !ScriptableObjectCollectionSettings.Exist();
        }

        private static int GetHasCount(Object[] objects)
        {
            int hasValue = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                Object targetObj = objects[i];

                if (targetObj == null)
                    continue;

                hasValue += targetObj.GetHashCode();
            }

            return hasValue;
        }

        public static bool IsFoldoutOpen(params Object[] objects)
        {
            int hashCount = GetHasCount(objects);
           
            if (hashCount == 0)
                return false;

            if(!objectToFoldOut.TryGetValue(hashCount, out bool value))
                objectToFoldOut.Add(hashCount, value);

            return value;
        }

        public static void SetFoldoutOpen(bool value, params Object[] objects)
        {
            int hashCount = GetHasCount(objects);

            objectToFoldOut[hashCount] = value;
        }
    }
}

