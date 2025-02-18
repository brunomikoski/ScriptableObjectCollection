using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class SOCollectionsProjectContextMenus
    {
        // ================================
        // ISOCItem (ScriptableObjectCollectionItem) commands
        // ================================

        [MenuItem("Assets/ScriptableObjectCollections/ISOCItem/Move to Different Collection", true)]
        private static bool ValidateMoveToDifferentCollection()
        {
            Object[] selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length == 0)
                return false;
            // Check that every selected object implements ISOCItem.
            foreach (Object obj in selectedObjects)
            {
                ISOCItem socItem = obj as ISOCItem;
                if (socItem == null)
                    return false;
            }
            return true;
        }

        [MenuItem("Assets/ScriptableObjectCollections/ISOCItem/Move to Different Collection")]
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

            // Get available collections for the item type.
            List<ScriptableObjectCollection> possibleCollections =
                CollectionsRegistry.Instance.GetCollectionsByItemType(items[0].GetType());
            if (possibleCollections == null || possibleCollections.Count == 0)
            {
                EditorUtility.DisplayDialog("Move to Different Collection", "No collections available.", "OK");
                return;
            }
            // Exclude the current collection of the first item.
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
            // Present a GenericMenu so the user can choose a new collection.
            GenericMenu menu = new GenericMenu();
            foreach (ScriptableObjectCollection collection in filteredCollections)
            {
                menu.AddItem(new GUIContent(collection.name), false, delegate
                {
                    foreach (ISOCItem item in items)
                    {
                        SOCItemUtility.MoveItem(item, collection);
                    }
                    EditorUtility.SetDirty(collection);
                });
            }
            menu.ShowAsContext();
        }

        [MenuItem("Assets/ScriptableObjectCollections/ISOCItem/Select Collection", true)]
        private static bool ValidateSelectCollection()
        {
            Object[] selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length != 1)
                return false;
            ISOCItem socItem = selectedObjects[0] as ISOCItem;
            return socItem != null && socItem.Collection != null;
        }

        [MenuItem("Assets/ScriptableObjectCollections/ISOCItem/Select Collection")]
        private static void SelectCollection()
        {
            Object[] selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length != 1)
                return;
            ISOCItem socItem = selectedObjects[0] as ISOCItem;
            if (socItem != null && socItem.Collection != null)
                Selection.activeObject = socItem.Collection;
        }

        [MenuItem("Assets/ScriptableObjectCollections/ISOCItem/Duplicate", true)]
        private static bool ValidateDuplicateItem()
        {
            Object[] selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length == 0)
                return false;
            foreach (Object obj in selectedObjects)
            {
                if (!(obj is ScriptableObject))
                    return false;
            }
            return true;
        }

        [MenuItem("Assets/ScriptableObjectCollections/ISOCItem/Duplicate")]
        private static void DuplicateItem()
        {
            Object[] selectedObjects = Selection.objects;
            foreach (Object obj in selectedObjects)
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(assetPath))
                    continue;
                string newPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                bool copySuccess = AssetDatabase.CopyAsset(assetPath, newPath);
                if (copySuccess)
                    AssetDatabase.Refresh();
            }
        }

        [MenuItem("Assets/ScriptableObjectCollections/ISOCItem/Delete", true)]
        private static bool ValidateDeleteItem()
        {
            Object[] selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length == 0)
                return false;
            foreach (Object obj in selectedObjects)
            {
                if (!(obj is ScriptableObject))
                    return false;
            }
            return true;
        }

        [MenuItem("Assets/ScriptableObjectCollections/ISOCItem/Delete")]
        private static void DeleteItem()
        {
            Object[] selectedObjects = Selection.objects;
            if (EditorUtility.DisplayDialog("Delete Item",
                "Are you sure you want to delete the selected item(s)?", "Yes", "No"))
            {
                List<ScriptableObjectCollection> objectCollections = new List<ScriptableObjectCollection>();
                foreach (Object obj in selectedObjects)
                {
                    string assetPath = AssetDatabase.GetAssetPath(obj);
                    if(obj is ISOCItem socItem)
                        objectCollections.Add(socItem.Collection);

                    if (!string.IsNullOrEmpty(assetPath))
                        AssetDatabase.DeleteAsset(assetPath);
                }

                foreach (ScriptableObjectCollection objectCollection in objectCollections)
                {
                    objectCollection.RefreshCollection();
                }

                AssetDatabase.Refresh();
            }
        }

        // ================================
        // ScriptableObjectCollection commands
        // ================================

        [MenuItem("Assets/ScriptableObjectCollections/ScriptableObjectCollection/Duplicate Collection", true)]
        private static bool ValidateDuplicateCollection()
        {
            Object[] selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length != 1)
                return false;
            return selectedObjects[0] is ScriptableObjectCollection;
        }

        [MenuItem("Assets/ScriptableObjectCollections/ScriptableObjectCollection/Duplicate Collection")]
        private static void DuplicateCollection()
        {
            Object[] selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length != 1)
                return;
            ScriptableObjectCollection originalCollection = selectedObjects[0] as ScriptableObjectCollection;
            if (originalCollection == null)
                return;
            string assetPath = AssetDatabase.GetAssetPath(originalCollection);
            if (string.IsNullOrEmpty(assetPath))
                return;
            string newPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            bool copySuccess = AssetDatabase.CopyAsset(assetPath, newPath);
            if (copySuccess)
            {
                AssetDatabase.Refresh();
                CollectionsRegistry.Instance.ValidateCollections();
                EditorUtility.DisplayDialog("Duplicate Collection",
                    "Collection duplicated successfully at:\n" + newPath, "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Duplicate Collection",
                    "Failed to duplicate collection.", "OK");
            }
        }
    }
}