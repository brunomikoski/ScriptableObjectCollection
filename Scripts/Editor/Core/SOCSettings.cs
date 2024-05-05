using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [Serializable]
    public class SOCSettings
    {
        [SerializeField]
        public List<CollectionSettings> collectionSettings = new();
        
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
        internal string generatedScriptsDefaultFilePath = @"Assets\Generated\Scripts";

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


        public string GetParentFolderPathForCollection(ScriptableObjectCollection collection)
        {
            CollectionSettings settings = GetOrCreateCollectionSettings(collection);
            if (!string.IsNullOrEmpty(settings.ParentFolderPath) && AssetDatabase.IsValidFolder(settings.ParentFolderPath))
            {
                return settings.ParentFolderPath;
            }

            return string.Empty;
        }
        public DefaultAsset GetParentDefaultAssetScriptsFolderForCollection(ScriptableObjectCollection collection)
        {
            CollectionSettings settings = GetOrCreateCollectionSettings(collection);
            if (!string.IsNullOrEmpty(settings.ParentFolderPath) &&
                AssetDatabase.IsValidFolder(settings.ParentFolderPath))
            {
                return AssetDatabase.LoadAssetAtPath<DefaultAsset>(settings.ParentFolderPath);
            }
            
            return null;
        }
        
        public void SetGeneratedScriptsParentFolder(ScriptableObjectCollection collection, Object evtNewValue)
        {
            string assetPath = string.Empty;
            if (evtNewValue != null)
                assetPath = AssetDatabase.GetAssetPath(evtNewValue);


            CollectionSettings settings = GetOrCreateCollectionSettings(collection);
            settings.ParentFolderPath = assetPath;
            
            Save();
        }
        
        public bool GetUseBaseClassForItem(ScriptableObjectCollection collection)
        {
            return GetOrCreateCollectionSettings(collection).UseBaseClassForItems;
        }
        
        public void SetUsingBaseClassForItems(ScriptableObjectCollection collection, bool useBaseClass)
        {
            CollectionSettings settings = GetOrCreateCollectionSettings(collection);
            settings.UseBaseClassForItems = useBaseClass;
            Save();
        }
        

        public bool GetWriteAsPartialClass(ScriptableObjectCollection collection)
        {
            CollectionSettings settings = GetOrCreateCollectionSettings(collection);
            if (settings.WriteAsPartialClass && CodeGenerationUtility.CheckIfCanBePartial(collection))
                return true;

            SetWriteAsPartialClass(collection, false);
            
            return false;
        }
        
        public void SetWriteAsPartialClass(ScriptableObjectCollection collection, bool writeAsPartial)
        {
            CollectionSettings settings = GetOrCreateCollectionSettings(collection);
            if (settings.WriteAsPartialClass == writeAsPartial)
                return;
            
            settings.WriteAsPartialClass = writeAsPartial;
            Save();
        }
        
        public string GetNamespaceForCollection(ScriptableObjectCollection collection)
        {
            return GetOrCreateCollectionSettings(collection).Namespace;
        }

        public void SetNamespaceForCollection(ScriptableObjectCollection collection, string targetNamespace)
        {
            CollectionSettings settings = GetOrCreateCollectionSettings(collection);
            settings.Namespace = targetNamespace;
            Save();
        }

        public string GetStaticFilenameForCollection(ScriptableObjectCollection collection)
        {
            return GetOrCreateCollectionSettings(collection).StaticFilename;
        }

        public void SetStaticFilenameForCollection(ScriptableObjectCollection collection, string targetNewName)
        {
            CollectionSettings settings = GetOrCreateCollectionSettings(collection);
            settings.StaticFilename = targetNewName;
            Save();
        }
        
        public bool GetEnforceIndirectAccess(ScriptableObjectCollection collection)
        {
            return GetOrCreateCollectionSettings(collection).EnforceIndirectAccess;
        }
        
        public void SetEnforceIndirectAccess(ScriptableObjectCollection collection, bool enforceIndirectAccess)
        {
            CollectionSettings settings = GetOrCreateCollectionSettings(collection);
            settings.EnforceIndirectAccess = enforceIndirectAccess;
            Save();
        }
        
        public CollectionSettings GetOrCreateCollectionSettings(ScriptableObjectCollection collection)
        {
            CollectionSettings collectionSetting = null;
            for (int i = 0; i < collectionSettings.Count; i++)
            {
                collectionSetting = collectionSettings[i];
                if (collectionSetting.Guid == collection.GUID)
                {
                    return collectionSetting;
                }
            }

            collectionSetting = new CollectionSettings(collection);
            collectionSettings.Add(collectionSetting);
            Save();
            return collectionSetting;
        }

        public void ResetSettings(ScriptableObjectCollection collection)
        {
            for (int i = 0; i < collectionSettings.Count; i++)
            {
                CollectionSettings collectionSetting = collectionSettings[i];
                if (collectionSetting.Guid != collection.GUID) 
                    continue;
                
                collectionSettings.RemoveAt(i);
                Save();
                return;
            }
        }
    }
}
