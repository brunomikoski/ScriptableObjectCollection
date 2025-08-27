using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public class MoveToCollectionWindow : EditorWindow
    {
        private List<ISOCItem> itemsToMove;
        private List<ScriptableObjectCollection> availableCollections;

        public static void ShowWindow(List<ISOCItem> items, List<ScriptableObjectCollection> collections)
        {
            MoveToCollectionWindow window = GetWindow<MoveToCollectionWindow>("Move to Collection");
            window.itemsToMove = items;
            window.availableCollections = collections;
            window.ShowPopup();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Select a Collection to Move Items", EditorStyles.boldLabel);

            if (availableCollections == null || availableCollections.Count == 0)
            {
                EditorGUILayout.LabelField("No available collections.");
                return;
            }

            foreach (ScriptableObjectCollection collection in availableCollections)
            {
                if (GUILayout.Button(collection.name))
                {
                    foreach (ISOCItem item in itemsToMove)
                    {
                        SOCItemUtility.MoveItem(item, collection);
                        EditorUtility.SetDirty(collection);
                    }

                    Close();
                }
            }
        }
    }
}