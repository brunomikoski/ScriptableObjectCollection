using System;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections 
{
    [CustomPropertyDrawer(typeof(CollectionAdvancedSettings), true)]
    public sealed class CollectionAdvancedSettingsPropertyDrawer : PropertyDrawer 
    {
        private bool initialized = false;
        private ScriptableObjectCollection collection;
        private SerializedProperty targetProperty;
        
        private void Initialize(SerializedProperty property) 
        {
            this.collection = property.serializedObject.targetObject as ScriptableObjectCollection;
            this.targetProperty = property;
            
            CheckGeneratedCodeLocation();
            CheckGeneratedStaticFileName();

            this.initialized = true;
        }

        private void CheckGeneratedStaticFileName()
        {
            SerializedProperty fileNameSerializedProperty = targetProperty.DeepFindPropertyRelative("GeneratedStaticClassFileName");
            if (!string.IsNullOrEmpty(fileNameSerializedProperty.stringValue) || collection == null)
                return;
            
            if (collection.name.Equals(collection.GetItemType().Name, StringComparison.Ordinal) 
                && targetProperty.DeepFindPropertyRelative("GenerateAsPartialClass").boolValue)
            {
                fileNameSerializedProperty.stringValue = $"{collection.GetItemType().Name}Static";
            }
            else
            {
                fileNameSerializedProperty.stringValue = $"{collection.name}Static".Sanitize().FirstToUpper();
            }
            fileNameSerializedProperty.serializedObject.ApplyModifiedProperties();
        }

        private void CheckGeneratedCodeLocation()
        {
            SerializedProperty generatedCodePathSerializedProperty = targetProperty.DeepFindPropertyRelative("GeneratedFileLocationPath");
            if (!string.IsNullOrEmpty(generatedCodePathSerializedProperty.stringValue))
                return;

            ScriptableObjectCollectionSettings settingsInstance = ScriptableObjectCollectionSettings.GetInstance();
            if (!string.IsNullOrEmpty(settingsInstance.DefaultGeneratedScriptsPath))
            {
                generatedCodePathSerializedProperty.stringValue = settingsInstance.DefaultGeneratedScriptsPath;
                generatedCodePathSerializedProperty.serializedObject.ApplyModifiedProperties();
            }
            else if (collection)
            {
                string collectionScriptPath =
                    Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(collection)));
                
                generatedCodePathSerializedProperty.stringValue = collectionScriptPath;
                generatedCodePathSerializedProperty.serializedObject.ApplyModifiedProperties();
            }
        }

        private bool CheckIfCanBePartial() 
        {
            if (collection == null) 
            {
                // If there's no collection, assume it can be partial, so the user can still set a value when creating
                // a collection via advanced settings.
                return true;
            }
            
            SerializedProperty generatedCodePathSerializedProperty = targetProperty.DeepFindPropertyRelative("GeneratedFileLocationPath");
            SerializedProperty usePartialClassSerializedProperty = targetProperty.DeepFindPropertyRelative("GenerateAsPartialClass");
            string baseClassPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(collection));
            string baseAssembly = CompilationPipeline.GetAssemblyNameFromScriptPath(baseClassPath);
            string targetGeneratedCodePath = CompilationPipeline.GetAssemblyNameFromScriptPath(generatedCodePathSerializedProperty.stringValue);
            
            // NOTE: If you're not using assemblies for your code, it's expected that 'targetGeneratedCodePath' would
            // be the same as 'baseAssembly', but it isn't. 'targetGeneratedCodePath' seems to be empty in that case.
            bool canBePartial = baseAssembly.Equals(targetGeneratedCodePath, StringComparison.Ordinal) ||
                                string.IsNullOrEmpty(targetGeneratedCodePath);
            
            if (usePartialClassSerializedProperty.boolValue && !canBePartial)
            {
                usePartialClassSerializedProperty.boolValue = false;
                usePartialClassSerializedProperty.serializedObject.ApplyModifiedProperties();
            }

            return canBePartial;
        }

        private void DrawAutomaticallyLoaded() 
        {
            SerializedProperty automaticLoadedSerializedProperty = targetProperty.DeepFindPropertyRelative("AutomaticallyLoaded");
            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                bool isAutomaticallyLoaded = EditorGUILayout.Toggle("Automatically Loaded", automaticLoadedSerializedProperty.boolValue);
                if (changeCheck.changed)
                {
                    automaticLoadedSerializedProperty.boolValue = isAutomaticallyLoaded;
                    automaticLoadedSerializedProperty.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void DrawGeneratedClassParentFolder() 
        {
            SerializedProperty generatedCodePathSerializedProperty = targetProperty.DeepFindPropertyRelative("GeneratedFileLocationPath");
            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                DefaultAsset pathObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>(generatedCodePathSerializedProperty.stringValue);
                if (pathObject == null && !string.IsNullOrEmpty(ScriptableObjectCollectionSettings.GetInstance().DefaultGeneratedScriptsPath))
                {
                    pathObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>(ScriptableObjectCollectionSettings
                        .GetInstance().DefaultGeneratedScriptsPath);
                }
                
                pathObject = (DefaultAsset) EditorGUILayout.ObjectField(
                    "Generated Scripts Parent Folder",
                    pathObject,
                    typeof(DefaultAsset),
                    false
                );
                string assetPath = AssetDatabase.GetAssetPath(pathObject);

                if (changeCheck.changed || !string.Equals(generatedCodePathSerializedProperty.stringValue, assetPath, StringComparison.Ordinal))
                {
                    generatedCodePathSerializedProperty.stringValue = assetPath;
                    generatedCodePathSerializedProperty.serializedObject.ApplyModifiedProperties();

                    if (string.IsNullOrEmpty(ScriptableObjectCollectionSettings.GetInstance().DefaultGeneratedScriptsPath))
                        ScriptableObjectCollectionSettings.GetInstance().SetDefaultGeneratedScriptsPath(assetPath);
                }
            }
        }
        
        private void DrawGeneratedFileName()
        {
            SerializedProperty fileNameSerializedProperty = targetProperty.DeepFindPropertyRelative("GeneratedStaticClassFileName");
            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                string newFileName = EditorGUILayout.DelayedTextField("Static File Name", fileNameSerializedProperty.stringValue);
                if (changeCheck.changed)
                {
                    fileNameSerializedProperty.stringValue = newFileName;
                    fileNameSerializedProperty.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void DrawGeneratedFileNamespace()
        {
            SerializedProperty fileNamespaceSerializedProperty = targetProperty.DeepFindPropertyRelative("GenerateStaticFileNamespace");
            if (string.IsNullOrEmpty(fileNamespaceSerializedProperty.stringValue))
            {
                if (collection != null)
                {
                    string targetNamespace = collection.GetItemType().Namespace;
                    if (!string.IsNullOrEmpty(targetNamespace))
                    {
                        fileNamespaceSerializedProperty.stringValue = targetNamespace;
                        fileNamespaceSerializedProperty.serializedObject.ApplyModifiedProperties();
                    }
                }
            }
            
            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                string newFileName = EditorGUILayout.DelayedTextField("Namespace", fileNamespaceSerializedProperty.stringValue);
                if (changeCheck.changed)
                {
                    fileNamespaceSerializedProperty.stringValue = newFileName;
                    fileNamespaceSerializedProperty.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void DrawPartialClassToggle()
        {
            SerializedProperty usePartialClassSerializedProperty = targetProperty.DeepFindPropertyRelative("GenerateAsPartialClass");
            bool canBePartial = CheckIfCanBePartial();
            
            EditorGUI.BeginDisabledGroup(!canBePartial);
            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                bool writeAsPartial = EditorGUILayout.Toggle("Write as Partial Class", usePartialClassSerializedProperty.boolValue);
                if (changeCheck.changed)
                {
                    usePartialClassSerializedProperty.boolValue = writeAsPartial;
                    usePartialClassSerializedProperty.serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUI.EndDisabledGroup();
        }
        
        private void DrawUseBaseClassToggle()
        {
            SerializedProperty useBaseClassProperty = targetProperty.DeepFindPropertyRelative("GenerateAsBaseClass");
    
            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                bool useBaseClass = EditorGUILayout.Toggle("Use Base Class for items", useBaseClassProperty.boolValue);
                if (changeCheck.changed)
                {
                    useBaseClassProperty.boolValue = useBaseClass;
                    useBaseClassProperty.serializedObject.ApplyModifiedProperties();
                }
            }
        }        
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) 
        {
            if (!this.initialized) 
            {
                this.Initialize(property);
            }
            
            DrawAutomaticallyLoaded();
            DrawGeneratedClassParentFolder();
            DrawPartialClassToggle();
            DrawUseBaseClassToggle();
            DrawGeneratedFileName();
            DrawGeneratedFileNamespace();
        }
    }
}
