using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public sealed class DeleteCollectionEditorWindow : EditorWindow
    {
        private ScriptableObjectCollection targetCollection;
        private bool deleteCollectionItems = true;

        private void SetTargetCollection(ScriptableObjectCollection targetCollection)
        {
            this.targetCollection = targetCollection;
        }

        private void OnGUI()
        {
            if (targetCollection == null)
            {
                Close();
                GUIUtility.ExitGUI();
                return;
            }
            
            EditorGUILayout.LabelField($"Are you sure you want to delete {targetCollection.name}?");

            deleteCollectionItems = EditorGUILayout.ToggleLeft("Delete Collection Items", deleteCollectionItems);

            EditorGUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUILayout.HorizontalScope())
            {
                Color backgroundColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Delete Collection"))
                {
                    CollectionsRegistry.Instance.UnregisterCollection(targetCollection);

                    if (deleteCollectionItems)
                    {
                        for (int i = targetCollection.Items.Count - 1; i >= 0; i--)
                        {
                            ScriptableObject collectionItem =
                                targetCollection.Items[i];

                            targetCollection.Remove(collectionItem);
                            
                            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(collectionItem));
                        }
                    }

                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(targetCollection));
                    
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                }
                GUI.backgroundColor = backgroundColor;
            
                if (GUILayout.Button("Cancel"))
                {
                    Close();
                    GUIUtility.ExitGUI();
                }
            }

        }

        public static void DeleteCollection(ScriptableObjectCollection targetCollection)
        {
            DeleteCollectionEditorWindow editorWindow = CreateInstance<DeleteCollectionEditorWindow>();
            editorWindow.SetTargetCollection(targetCollection);
            editorWindow.titleContent = new GUIContent("Delete Collection");
            editorWindow.position = new Rect(Screen.width * 0.5f, Screen.height * .05f, 300, 80);
            editorWindow.Show();
        }
    }
}
