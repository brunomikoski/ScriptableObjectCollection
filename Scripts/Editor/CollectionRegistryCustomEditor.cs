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
                    SerializedObject serializedObject = new SerializedObject(collection);
                    using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("automaticallyLoaded"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("staticFileGenerationType"));

                        if (changeCheck.changed)
                        {
                            ObjectUtility.SetDirty(collection);
                            serializedObject.ApplyModifiedProperties();
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}