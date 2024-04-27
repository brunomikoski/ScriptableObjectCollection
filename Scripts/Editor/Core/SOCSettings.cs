using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    
    [Serializable]
    public class SOCSettings
    {
        [Serializable]
        public class NamespaceSettings
        {
            public LongGuid CollectionGuid;
            public string Namespace;
        }
        
        [Serializable]
        public class GeneratedScriptsParentFolder
        {
            public LongGuid CollectionGuid;
            public string ParentFolderAssetPath;
        }
        
        [Serializable]
        public class StaticFilenameSettings
        {
            public LongGuid CollectionGuid;
            public string StaticFilename;
        }
        
        private const string STORAGE_PATH = "ProjectSettings/ScriptableObjectCollection.json";
        private const int MINIMUM_NAMESPACE_DEPTH = 1;

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            SettingsProvider provider = new("Project/Scriptable Object Collection", SettingsScope.Project)
            {
                label = "Scriptable Object Collection",
                guiHandler = Instance.OnSceneGUI,
                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Editor", "SOC", "Scriptable Objects", "Scriptable Objects Collection" })
            };

            return provider;
        }
        
        private static SOCSettings instance;
        public static SOCSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    if (File.Exists(STORAGE_PATH))
                    {
                        // Load settings from file.
                        string json = File.ReadAllText(STORAGE_PATH);
                        instance = JsonUtility.FromJson<SOCSettings>(json);
                    }
                    else
                    {
                        // Create new settings instance if file doesn't exist.
                        instance = new SOCSettings();
                    }
                }
                return instance;
            }
        }

        [SerializeField]
        private string namespacePrefix;
        public string NamespacePrefix => namespacePrefix;
        
        [SerializeField]
        private bool useMaximumNamespaceDepth = true;
        public bool UseMaximumNamespaceDepth => useMaximumNamespaceDepth;

        [SerializeField] 
        private int maximumNamespaceDepth = 2;
        public int MaximumNamespaceDepth => maximumNamespaceDepth;
        
        [SerializeField]
        private string generatedScriptsDefaultFilePath = @"Assets\Generated\Scripts";
        public string GeneratedScriptsDefaultFilePath => generatedScriptsDefaultFilePath;
        
        [SerializeField]
        private List<LongGuid> useCustomEditorDrawer = new List<LongGuid>();


        [SerializeField]
        private List<GeneratedScriptsParentFolder> generatedScriptsParentFolders = new List<GeneratedScriptsParentFolder>();

        
        [SerializeField]
        private List<LongGuid> writingGeneratedCodeAsPartialClass = new List<LongGuid>();

        [SerializeField]
        private List<LongGuid> usingBaseClassForItems = new List<LongGuid>();

        [SerializeField]
        private List<StaticFilenameSettings> staticFilenameSettings = new List<StaticFilenameSettings>();


        [SerializeField]
        private List<NamespaceSettings> namespaceSettings = new List<NamespaceSettings>();

        
        private static readonly GUIContent namespacePrefixGUIContent = new GUIContent(
            "Prefix",
            "When using the Create New Collection wizard," +
            "the namespace will always start with this value. Usually the name of the company.");
        
        private static readonly GUIContent namespaceUseMaxDepthGUIContent = new GUIContent(
            "Maximum Depth",
            "If specified, automatically derived namespaces will only include up to this many folders inside your " +
            "project's Scripts folder.");

        
        [Obsolete("Default Namespace has been renamed to Namespace Prefix. Please use the corresponding function.")]
        public void SetDefaultNamespace(string namespacePrefix)
        {
            SetNamespacePrefix(namespacePrefix);
            Save();
        }
        
        public void SetNamespacePrefix(string namespacePrefix)
        {
            this.namespacePrefix = namespacePrefix;
            Save();
        }
        
        public void SetUseMaximumNamespaceDepth(bool useMaximumNamespaceDepth)
        {
            this.useMaximumNamespaceDepth = useMaximumNamespaceDepth;
            Save();
        }
        
        public void SetMaximumNamespaceDepth(int maximumNamespaceDepth)
        {
            this.maximumNamespaceDepth = Mathf.Max(MINIMUM_NAMESPACE_DEPTH, maximumNamespaceDepth);
            Save();
        }

        public void OnSceneGUI(string search)
        {
            EditorGUILayout.LabelField("Namespaces", EditorStyles.boldLabel);
            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                string newNamespacePrefix = EditorGUILayout.DelayedTextField(
                    namespacePrefixGUIContent, namespacePrefix);

                EditorGUILayout.BeginHorizontal();
                useMaximumNamespaceDepth = EditorGUILayout.Toggle("Use Maximum Namespace Depth",
                    useMaximumNamespaceDepth, GUILayout.Width(EditorGUIUtility.labelWidth + 16));

                bool wasGuiEnabled = GUI.enabled;
                GUI.enabled = useMaximumNamespaceDepth;
                int newMaximumNamespaceDepth = EditorGUILayout.DelayedIntField(
                    GUIContent.none, maximumNamespaceDepth);
                GUI.enabled = wasGuiEnabled;
                EditorGUILayout.EndHorizontal();
                
                if (changeCheck.changed)
                {
                    namespacePrefix = newNamespacePrefix;
                    maximumNamespaceDepth = newMaximumNamespaceDepth;
                    Save();
                }
            }
            
            EditorGUILayout.LabelField("Default Generated Scripts Folder", EditorStyles.boldLabel);
            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                DefaultAsset pathObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>(generatedScriptsDefaultFilePath);
                
                pathObject = (DefaultAsset) EditorGUILayout.ObjectField(
                    "Generated Scripts Parent Folder",
                    pathObject,
                    typeof(DefaultAsset),
                    false
                );
                string assetPath = AssetDatabase.GetAssetPath(pathObject);

                if (changeCheck.changed)
                {
                    generatedScriptsDefaultFilePath = assetPath;
                    Save();
                }
            }
        }

        public void SetGeneratedScriptsDefaultFilePath(string assetPath)
        {
            generatedScriptsDefaultFilePath = assetPath;
            Save();
        }

        public void Save()
        {
            string json = EditorJsonUtility.ToJson(this, prettyPrint: true);
            File.WriteAllText(STORAGE_PATH, json);
        }


        public bool ShouldDrawUsingCustomEditor(ScriptableObjectCollection collection)
        {
            return useCustomEditorDrawer.Contains(collection.GUID);
        }
        
        public void SetUseCustomEditor(ScriptableObjectCollection collection, bool useCustomEditor)
        {
            if (useCustomEditor)
            {
                if (!useCustomEditorDrawer.Contains(collection.GUID))
                {
                    useCustomEditorDrawer.Add(collection.GUID);
                    Save();
                }
            }
            else
            {
                if (useCustomEditorDrawer.Contains(collection.GUID))
                {
                    useCustomEditorDrawer.Remove(collection.GUID);
                    Save();
                }
            }
        }

        private GeneratedScriptsParentFolder GetOrCreatedGeneratedScriptsFolderSettings(
            ScriptableObjectCollection collection)
        {
            for (int i = 0; i < generatedScriptsParentFolders.Count; i++)
            {
                GeneratedScriptsParentFolder generatedScriptsParentFolder = generatedScriptsParentFolders[i];
                if (generatedScriptsParentFolder.CollectionGuid == collection.GUID)
                {
                    return generatedScriptsParentFolder;
                }
            }
            
            GeneratedScriptsParentFolder newGeneratedScriptsParentFolder = new GeneratedScriptsParentFolder()
            {
                CollectionGuid = collection.GUID,
                ParentFolderAssetPath = generatedScriptsDefaultFilePath
            };
            generatedScriptsParentFolders.Add(newGeneratedScriptsParentFolder);
            return newGeneratedScriptsParentFolder;
        }
        
        public DefaultAsset GetGeneratedScriptsParentFolder(ScriptableObjectCollection collection)
        {
            foreach (GeneratedScriptsParentFolder generatedScriptsParentFolder in generatedScriptsParentFolders)
            {
                if (generatedScriptsParentFolder.CollectionGuid == collection.GUID)
                {
                    if (!string.IsNullOrEmpty(generatedScriptsParentFolder.ParentFolderAssetPath))
                    {
                        return AssetDatabase.LoadAssetAtPath<DefaultAsset>(generatedScriptsParentFolder.ParentFolderAssetPath);
                    }

                    return null;
                }
            }

            return AssetDatabase.LoadAssetAtPath<DefaultAsset>(generatedScriptsDefaultFilePath);
        }

        public void SetGeneratedScriptsParentFolder(ScriptableObjectCollection collection, Object evtNewValue)
        {
            string assetPath = string.Empty;
            if (evtNewValue != null)
                assetPath = AssetDatabase.GetAssetPath(evtNewValue);

            GeneratedScriptsParentFolder settings = GetOrCreatedGeneratedScriptsFolderSettings(collection);
            settings.ParentFolderAssetPath = assetPath;
            
            Save();
        }
        
        
        public bool GetUseBaseClassForITems(ScriptableObjectCollection collection)
        {
            return usingBaseClassForItems.Contains(collection.GUID);
        }
        
        public void SetUsingBaseClassForItems(ScriptableObjectCollection collection, bool useBaseClass)
        {
            if (useBaseClass)
            {
                if (!usingBaseClassForItems.Contains(collection.GUID))
                    usingBaseClassForItems.Add(collection.GUID);
            }
            else
            {
                usingBaseClassForItems.Remove(collection.GUID);
            }
            
            Save();
        }
        

        public bool GetWriteAsPartialClass(ScriptableObjectCollection collection)
        {
            bool canBePartial= CodeGenerationUtility.CheckIfCanBePartial(collection);
            if (!canBePartial)
            {
                writingGeneratedCodeAsPartialClass.Remove(collection.GUID);
                return false;
            }
            
            return writingGeneratedCodeAsPartialClass.Contains(collection.GUID);
        }
        
        public void SetWriteAsPartialClass(ScriptableObjectCollection collection, bool writeAsPartial)
        {
            if (!CodeGenerationUtility.CheckIfCanBePartial(collection))
                return;
            
            if (writeAsPartial)
            {
                if (!writingGeneratedCodeAsPartialClass.Contains(collection.GUID))
                    writingGeneratedCodeAsPartialClass.Add(collection.GUID);
            }
            else
            {
                writingGeneratedCodeAsPartialClass.Remove(collection.GUID);
            }
            
            Save();
        }
        
        public string GetNamespaceForCollection(ScriptableObjectCollection collection)
        {
            return GetOrCreateNamespaceForCollection(collection).Namespace;
        }

        public void SetNamespaceForCollection(ScriptableObjectCollection collection, string targetNamespace)
        {
            NamespaceSettings settings = GetOrCreateNamespaceForCollection(collection);
            settings.Namespace = targetNamespace;
            Save();
        }

        private NamespaceSettings GetOrCreateNamespaceForCollection(ScriptableObjectCollection collection)
        {
            NamespaceSettings namespaceSetting = null;
            for (int i = 0; i < namespaceSettings.Count; i++)
            {
                namespaceSetting = namespaceSettings[i];
                if (namespaceSetting.CollectionGuid == collection.GUID)
                {
                    return namespaceSetting;
                }
            }

            namespaceSetting = new NamespaceSettings();
            namespaceSetting.CollectionGuid = collection.GUID;

            string targetNamespace = collection.GetItemType().Namespace;
            if (!string.IsNullOrEmpty(namespacePrefix))
                targetNamespace = $"{namespacePrefix}.{targetNamespace}";
            
            namespaceSetting.Namespace = targetNamespace;
            namespaceSettings.Add(namespaceSetting);
            Save();
            return namespaceSetting;
        }


        public string GetStaticFilenameForCollection(ScriptableObjectCollection collection)
        {
            return GetOrCreateStaticFilenameForCollection(collection).StaticFilename;
        }

        private StaticFilenameSettings GetOrCreateStaticFilenameForCollection(ScriptableObjectCollection collection)
        {
            StaticFilenameSettings staticFilenameSetting = null;
            for (int i = 0; i < staticFilenameSettings.Count; i++)
            {
                staticFilenameSetting = staticFilenameSettings[i];
                if (staticFilenameSetting.CollectionGuid == collection.GUID)
                {
                    return staticFilenameSetting;
                }
            }

            staticFilenameSetting = new StaticFilenameSettings();
            staticFilenameSetting.CollectionGuid = collection.GUID;

            if (!CodeGenerationUtility.CheckIfCanBePartial(collection))
                staticFilenameSetting.StaticFilename = $"{collection.name}Static".FirstToUpper().Sanitize();
            else 
                staticFilenameSetting.StaticFilename = $"{collection.name}".FirstToUpper().Sanitize();
            
            staticFilenameSettings.Add(staticFilenameSetting);
            Save();
            return staticFilenameSetting;
        }

        public void SetStaticFilenameForCollection(ScriptableObjectCollection collection, string targetNewName)
        {
            StaticFilenameSettings staticFilenameSetting = GetOrCreateStaticFilenameForCollection(collection);
            staticFilenameSetting.StaticFilename = targetNewName;
            Save();
        }
    }
}
