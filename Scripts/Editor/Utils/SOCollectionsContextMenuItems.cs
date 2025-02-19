using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class SOCollectionsProjectContextMenus
    {
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