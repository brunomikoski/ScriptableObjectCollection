using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class SOCollectionsProjectContextMenus
    {
        [MenuItem("Tools/ScriptableObjectCollection/Generate All Static Access Files", priority = 2000)]
        private static void GenerateAllStaticAccessFiles()
        {
            CollectionsRegistry.Instance.ReloadCollections();

            int generatedCount = 0;
            IReadOnlyList<ScriptableObjectCollection> collections = CollectionsRegistry.Instance.Collections;
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection collection = collections[i];
                if (!CodeGenerationUtility.DoesStaticFileForCollectionExist(collection))
                    continue;

                CodeGenerationUtility.GenerateStaticCollectionScript(collection);
                generatedCount++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[SOC] Generated static access files for {generatedCount} collections.");
        }

        [MenuItem("Tools/ScriptableObjectCollection/Generate All Static Access Files", validate = true)]
        private static bool Validate_GenerateAllStaticAccessFiles()
        {
            return !EditorApplication.isPlaying;
        }
        
        
        [MenuItem("Tools/ScriptableObjectCollection/Generate Indirect Access for All Collection", priority = 2000)]
        private static void GenerateIndirectAccessToAllKnowCollection()
        {
            CollectionsRegistry.Instance.ReloadCollections();

            int generatedCount = 0;
            IReadOnlyList<ScriptableObjectCollection> collections = CollectionsRegistry.Instance.Collections;
            for (int i = 0; i < collections.Count; i++)
            {
                ScriptableObjectCollection collection = collections[i];

                CodeGenerationUtility.GenerateIndirectAccessForCollectionItemType(collection.GetItemType());
                generatedCount++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[SOC] Generated indirect access files for {generatedCount} collections.");
        }

        [MenuItem("Tools/ScriptableObjectCollection/Generate Indirect Access for All Collection", validate = true)]
        private static bool Validate_GenerateIndirectAccessToAllKnowCollection()
        {
            return !EditorApplication.isPlaying;
        }
        
        
        [MenuItem("Assets/Move to Different Collection", true, priority = 10000)]
        private static bool ValidateMoveToDifferentCollection()
        {
            Object[] selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length == 0)
                return false;

            foreach (Object obj in selectedObjects)
            {
                ISOCItem socItem = obj as ISOCItem;
                if (socItem == null)
                    return false;
            }

            List<ScriptableObjectCollection> possibleCollections =
                CollectionsRegistry.Instance.GetCollectionsByItemType(selectedObjects[0].GetType());

            if (possibleCollections == null || possibleCollections.Count <= 1)
            {
                return false;
            }

            return true;
        }

        [MenuItem("Assets/Move to Different Collection", priority = 10000)]
        private static void MoveToDifferentCollection()
        {
            Object[] selectedObjects = Selection.objects;
            List<ISOCItem> items = new List<ISOCItem>();

            foreach (Object obj in selectedObjects)
            {
                if (obj is ISOCItem item)
                    items.Add(item);
            }

            if (items.Count == 0)
                return;

            List<ScriptableObjectCollection> possibleCollections =
                CollectionsRegistry.Instance.GetCollectionsByItemType(items[0].GetType());

            if (possibleCollections == null || possibleCollections.Count == 0)
            {
                EditorUtility.DisplayDialog("Move to Different Collection", "No collections available.", "OK");
                return;
            }

            ScriptableObjectCollection currentCollection = items[0].Collection;

            List<ScriptableObjectCollection> filteredCollections = new List<ScriptableObjectCollection>();
            foreach (ScriptableObjectCollection collection in possibleCollections)
            {
                if (collection != currentCollection)
                    filteredCollections.Add(collection);
            }

            if (filteredCollections.Count == 0)
            {
                EditorUtility.DisplayDialog("Move to Different Collection", "No other collections available.", "OK");
                return;
            }

            MoveToCollectionWindow.ShowWindow(items, filteredCollections);
        }


        [MenuItem("Assets/Select Collection", true, priority = 10000)]
        private static bool ValidateSelectCollection()
        {
            Object[] selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length != 1)
                return false;
            ISOCItem socItem = selectedObjects[0] as ISOCItem;
            return socItem != null && socItem.Collection != null;
        }

        [MenuItem("Assets/Select Collection", priority = 10000)]
        private static void SelectCollection()
        {
            Object[] selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length != 1)
                return;
            ISOCItem socItem = selectedObjects[0] as ISOCItem;
            if (socItem != null && socItem.Collection != null)
                Selection.activeObject = socItem.Collection;
        }
    }
}
