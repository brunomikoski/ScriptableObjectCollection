using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public class ScriptableObjectCollectionSettings : ScriptableObjectForPreferences<ScriptableObjectCollectionSettings>
    {
        private const int MINIMUM_NAMESPACE_DEPTH = 1;
        
        [SerializeField]
        private string defaultGeneratedScriptsPath;
        public string DefaultGeneratedScriptsPath => defaultGeneratedScriptsPath;

        [FormerlySerializedAs("defaultNamespace")] [SerializeField]
        private string namespacePrefix = "CompanyName";
        public string NamespacePrefix => namespacePrefix;
        
        [SerializeField]
        private bool useMaximumNamespaceDepth = true;
        public bool UseMaximumNamespaceDepth => useMaximumNamespaceDepth;

        [SerializeField] private int maximumNamespaceDepth = 2;
        public int MaximumNamespaceDepth => maximumNamespaceDepth;

        private static readonly GUIContent namespacePrefixGUIContent = new GUIContent(
            "Prefix",
            "When using the Create New Collection wizard," +
            "the namespace will always start with this value. Usually the name of the company.");
        
        private static readonly GUIContent namespaceUseMaxDepthGUIContent = new GUIContent(
            "Maximum Depth",
            "If specified, automatically derived namespaces will only include up to this many folders inside your " +
            "project's Scripts folder.");

        [SettingsProvider]
        private static SettingsProvider SettingsProvider()
        {
            return CreateSettingsProvider("ScriptableObject Collection/Settings", OnSettingsGUI);
        }

        private void Changed()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }


        public void SetDefaultGeneratedScriptsPath(string targetPath)
        {
            defaultGeneratedScriptsPath = targetPath;
            Changed();
        }
        
        [Obsolete("Default Namespace has been renamed to Namespace Prefix. Please use the corresponding function.")]
        public void SetDefaultNamespace(string namespacePrefix)
        {
            SetNamespacePrefix(namespacePrefix);
            Changed();
        }
        
        public void SetNamespacePrefix(string namespacePrefix)
        {
            this.namespacePrefix = namespacePrefix;
            Changed();
        }
        
        public void SetUseMaximumNamespaceDepth(bool useMaximumNamespaceDepth)
        {
            this.useMaximumNamespaceDepth = useMaximumNamespaceDepth;
            Changed();
        }
        
        public void SetMaximumNamespaceDepth(int maximumNamespaceDepth)
        {
            this.maximumNamespaceDepth = Mathf.Max(MINIMUM_NAMESPACE_DEPTH, maximumNamespaceDepth);
            Changed();
        }

        private static void OnSettingsGUI(SerializedObject serializedObject)
        {
            SerializedProperty defaultGeneratedScriptsFolder = serializedObject.FindProperty("defaultGeneratedScriptsPath");
            DefaultAsset defaultAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(defaultGeneratedScriptsFolder
                .stringValue);

            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                DefaultAsset newFolder = EditorGUILayout.ObjectField("Default Scripts Folder", defaultAsset, typeof(DefaultAsset), false) as DefaultAsset;
                if (changeCheck.changed)
                {
                    defaultGeneratedScriptsFolder.stringValue = AssetDatabase.GetAssetPath(newFolder);
                    defaultGeneratedScriptsFolder.serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Namespaces", EditorStyles.boldLabel);
            SerializedProperty namespacePrefixSerializedProperty = serializedObject.FindProperty("namespacePrefix");
            SerializedProperty useMaximumNamespaceDepthSerializedProperty = serializedObject.FindProperty("useMaximumNamespaceDepth");
            SerializedProperty maximumNamespaceDepthSerializedProperty = serializedObject.FindProperty("maximumNamespaceDepth");
            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                string newNamespacePrefix = EditorGUILayout.DelayedTextField(
                    namespacePrefixGUIContent, namespacePrefixSerializedProperty.stringValue);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(
                    useMaximumNamespaceDepthSerializedProperty, namespaceUseMaxDepthGUIContent,
                    GUILayout.Width(EditorGUIUtility.labelWidth + 16));

                bool wasGuiEnabled = GUI.enabled;
                GUI.enabled = useMaximumNamespaceDepthSerializedProperty.boolValue;
                int newMaximumNamespaceDepth = EditorGUILayout.DelayedIntField(
                    GUIContent.none, maximumNamespaceDepthSerializedProperty.intValue);
                GUI.enabled = wasGuiEnabled;
                EditorGUILayout.EndHorizontal();
                
                if (changeCheck.changed)
                {
                    namespacePrefixSerializedProperty.stringValue = newNamespacePrefix;
                    maximumNamespaceDepthSerializedProperty.intValue = newMaximumNamespaceDepth;
                    namespacePrefixSerializedProperty.serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}
