using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Wizard for creating generator scripts.
    /// </summary>
    public sealed class GeneratorCreationWizard : EditorWindow
    {
        private const string GeneratorNameField = "NameInputField";
        private const string CollectionSuffix = "Collection";

        private string generatorName;
        private string templateName;

        private string targetFolderPath;
        
        private static Type collectionType;

        public static void Show(Type collectionType)
        {
            GeneratorCreationWizard.collectionType = collectionType;

            GeneratorCreationWizard generatorCreationWizard = GetWindow<GeneratorCreationWizard>("Create new generator");
            generatorCreationWizard.minSize = new Vector2(550, 95);
            generatorCreationWizard.maxSize = new Vector2(550, 95);

            generatorCreationWizard.InferGeneratorNames();
            generatorCreationWizard.InferFolder();

            generatorCreationWizard.ShowPopup();
        }

        private void InferGeneratorNames()
        {
            string baseName = collectionType.Name.RemoveSuffix(CollectionSuffix);

            // Add the 'generator' suffix.
            generatorName = baseName + "Generator";

            templateName = baseName + "Template";
        }

        private void InferFolder()
        {
            targetFolderPath = Path.GetDirectoryName(CreateCollectionWizard.LastGeneratedCollectionScriptPath.Value);
            
            // If we can find the script that the collection belongs to, default to that.
            if (ScriptUtility.TryGetScriptOfClass(collectionType, out MonoScript collectionScript))
            {
                targetFolderPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(collectionScript));
                targetFolderPath += "/Editor";
            }

            targetFolderPath = targetFolderPath.ToPathWithConsistentSeparators();
        }

        private void OnGUI()
        {
            if (collectionType == null)
            {
                Close();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Collection Type", GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUILayout.LabelField(collectionType.Name, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            
            GUI.SetNextControlName(GeneratorNameField);
            generatorName = EditorGUILayout.TextField("Generator Name", generatorName);

            targetFolderPath = EditorGUILayout.TextField("Script Folder", targetFolderPath);

            if (GUI.GetNameOfFocusedControl() != GeneratorNameField)
            {
                if (!string.IsNullOrEmpty(generatorName))
                    generatorName = generatorName.Sanitize();
            }

            EditorGUILayout.Space();
            
            using (new EditorGUI.DisabledScope(!AreSettingsValid()))
            {
                if (GUILayout.Button("Create"))
                    CreateGeneratorCodeFile();
            }
        }

        private void CreateGeneratorCodeFile()
        {
            string targetNamespace = String.Empty;
            if (!string.IsNullOrEmpty(collectionType.Namespace))
                targetNamespace = collectionType.Namespace;

            Dictionary<string, string> replacements = new Dictionary<string, string>
            {
                {"TemplateType", templateName},
                {"GeneratorType", generatorName},
                {"CollectionType", collectionType.Name},
            };
            string[] directives = {"System.Collections.Generic", "BrunoMikoski.ScriptableObjectCollections"};
            CodeGenerationUtility.CreateNewScript(
                generatorName,
                targetFolderPath, targetNamespace, directives, "GeneratorTemplate", replacements);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Open the new generator script.
            string generatorScriptPath = targetFolderPath + "/" + generatorName + ".cs";
            MonoScript generatorScript = AssetDatabase.LoadAssetAtPath<MonoScript>(generatorScriptPath);
            AssetDatabase.OpenAsset(generatorScript);

            Close();
        }

        private bool AreSettingsValid()
        {
            if (string.IsNullOrEmpty(generatorName))
                return false;

            if (targetFolderPath == null)
                return false;
            
            if (AssetDatabase.LoadAssetAtPath<MonoScript>(Path.Combine(targetFolderPath, $"{generatorName}.cs")) != null)
                return false;

            return true;
        }
    }
}
