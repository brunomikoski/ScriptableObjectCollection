using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomEditor(typeof(CollectionsRegistry))]
    public class CollectionRegistryCustomEditor : Editor
    {
        private SerializedProperty collections;

        private void OnEnable()
        {
            collections = serializedObject.FindProperty("collections");
        }
        public override void OnInspectorGUI()
        {
            DrawCollections();
        }

        private void DrawCollections()
        {
            for (int i = 0; i < collections.arraySize; i++)
            {
                DrawCollection((ScriptableObjectCollection) collections.GetArrayElementAtIndex(i).objectReferenceValue);
            }
        }

        private void DrawCollection(ScriptableObjectCollection collection)
        {
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    CollectionUtility.SetFoldoutOpen(collection,
                        EditorGUILayout.Toggle(GUIContent.none, CollectionUtility.IsFoldoutOpen(collection), 
                            EditorStyles.foldout,
                            GUILayout.Width(13)));

                    EditorGUILayout.LabelField(collection.name, CollectionEditorGUI.ItemNameStyle,
                        GUILayout.ExpandWidth(true));
                }
                
                if (CollectionUtility.IsFoldoutOpen(collection))
                {
                    EditorGUI.indentLevel++;

                    using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
                    {
                        bool isAutomaticallyLoaded = EditorGUILayout.ToggleLeft("Automatically Loaded",
                            ScriptableObjectCollectionSettings.Instance.IsCollectionAutomaticallyLoaded(collection));

                        if (changeCheck.changed)
                        {
                            ScriptableObjectCollectionSettings.Instance.SetCollectionAutomaticallyLoaded(collection,
                                isAutomaticallyLoaded);
                        }
                    }

                    using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
                    {
                        GeneratedStaticFileType staticCodeGeneratorType = (GeneratedStaticFileType)EditorGUILayout.EnumPopup("Static File Generator Type",
                            ScriptableObjectCollectionSettings.Instance.GetStaticFileTypeForCollection(collection));

                        if (changeCheck.changed)
                        {
                            ScriptableObjectCollectionSettings.Instance.SetStaticFileGeneratorTypeForCollection(collection,
                                staticCodeGeneratorType);
                        }
                    }

                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}
