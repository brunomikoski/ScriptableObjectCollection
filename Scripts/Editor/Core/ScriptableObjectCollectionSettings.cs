using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public class ScriptableObjectCollectionSettings : ScriptableObjectForPreferences<ScriptableObjectCollectionSettings>
    {
        [SerializeField]
        private string defaultGeneratedScriptsPath;
        public string DefaultGeneratedScriptsPath => defaultGeneratedScriptsPath;

        [SerializeField]
        private string defaultNamespace;
        public string DefaultNamespace => defaultNamespace;

        [SettingsProvider]
        private static SettingsProvider SettingsProvider()
        {
            return CreateSettingsProvider("ScriptableObject Collection/Settings", OnSettingsGUI);
        }


        public void SetDefaultGeneratedScriptsPath(string targetPath)
        {
            defaultGeneratedScriptsPath = targetPath;
        }
        
        public void SetDefaultNamespace(string targetNamespace)
        {
            defaultNamespace = targetNamespace;
        }
        
        private static void OnSettingsGUI(SerializedObject serializedObject)
        {
            SerializedProperty defaultGeneratedScriptsFolder = serializedObject.FindProperty("defaultGeneratedScriptsPath");
            SerializedProperty defaultNamespaceSerializedProperty = serializedObject.FindProperty("defaultNamespace");
            DefaultAsset defaultAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(defaultGeneratedScriptsFolder
                .stringValue);

            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                DefaultAsset newFolder = EditorGUILayout.ObjectField("Default Generated Scripts Folder", defaultAsset, typeof(DefaultAsset), false) as DefaultAsset;
                if (changeCheck.changed)
                {
                    defaultGeneratedScriptsFolder.stringValue = AssetDatabase.GetAssetPath(newFolder);
                    defaultGeneratedScriptsFolder.serializedObject.ApplyModifiedProperties();
                }
            }

            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                string newNamespace = EditorGUILayout.DelayedTextField("Default Namespace", defaultNamespaceSerializedProperty.stringValue);
                if (changeCheck.changed)
                {
                    defaultNamespaceSerializedProperty.stringValue = newNamespace;
                    defaultNamespaceSerializedProperty.serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}
