using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public sealed class CreateCollectionWizzard : EditorWindow
    {
        private const string LAST_COLLECTION_SCRIPTABLE_OBJECT_PATH_KEY = "CollectionScriptableObjectPathKey";
        private const string LAST_COLLECTION_FULL_NAME_KEY = "CollectionFullNameKey";
        private const string LAST_COLLECTION_SCRIPT_PATH_KEY = "CollectionScriptPathKey";

        private DefaultAsset cachedScriptableObjectFolder;
        private DefaultAsset ScriptableObjectFolder
        {
            get
            {
                if (cachedScriptableObjectFolder != null) 
                    return cachedScriptableObjectFolder;
                
                if (!string.IsNullOrEmpty(CollectionUtility.ScriptableObjectFolderPath))
                {
                    cachedScriptableObjectFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(
                        CollectionUtility.ScriptableObjectFolderPath);
                }
                return cachedScriptableObjectFolder;
            }
            set => cachedScriptableObjectFolder = value;
        }

        private DefaultAsset cachedScriptsFolder;
        private DefaultAsset ScriptsFolder
        {
            get
            {
                if (cachedScriptsFolder != null) 
                    return cachedScriptsFolder;
                if (!string.IsNullOrEmpty(CollectionUtility.ScriptsFolderPath))
                {
                    cachedScriptsFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(
                        CollectionUtility.ScriptsFolderPath);
                }
                return cachedScriptsFolder;
            }
            set => cachedScriptsFolder = value;
        }

        private string cachedNameSpace;
        private string TargetNameSpace
        {
            get
            {
                if (string.IsNullOrEmpty(cachedNameSpace))
                    cachedNameSpace = CollectionUtility.TargetNamespace;

                return cachedNameSpace;
            }
            set => cachedNameSpace = value;
        }


        private static string LastCollectionScriptableObjectPath
        {
            get => EditorPrefs.GetString(LAST_COLLECTION_SCRIPTABLE_OBJECT_PATH_KEY, String.Empty);
            set => EditorPrefs.SetString(LAST_COLLECTION_SCRIPTABLE_OBJECT_PATH_KEY, value);
        }

        private static string LastCollectionFullName
        {
            get => EditorPrefs.GetString(LAST_COLLECTION_FULL_NAME_KEY, String.Empty);
            set => EditorPrefs.SetString(LAST_COLLECTION_FULL_NAME_KEY, value);
        }

        private static string LastCollectionScriptPath
        {
            get => EditorPrefs.GetString(LAST_COLLECTION_SCRIPT_PATH_KEY, String.Empty);
            set => EditorPrefs.SetString(LAST_COLLECTION_SCRIPT_PATH_KEY, value);
        }

        private bool createFoldForThisCollection = true;
        private bool createFoldForThisCollectionScripts = true;

        private string collectionName = "Collection";
        private string collectableName = "Collectable";

        public static CreateCollectionWizzard GetWindowInstance()
        {
            return GetWindow<CreateCollectionWizzard>("Creating New Collection");
        }
        
        public new static void Show()
        {
            CreateCollectionWizzard createCollectionWizzard = GetWindowInstance();
            createCollectionWizzard.ShowPopup();
        }

        private void OnGUI()
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling))
            {
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    using (new EditorGUILayout.VerticalScope("Box"))
                    {
                        EditorGUILayout.LabelField("Settings", EditorStyles.foldoutHeader);
                        EditorGUILayout.Space();

                        collectableName = EditorGUILayout.TextField("Collectable Name", collectableName);

                        collectionName = EditorGUILayout.TextField("Collection Name", collectionName);
                    }
                    using (new EditorGUILayout.VerticalScope("Box"))
                    {
                        EditorGUILayout.LabelField("Scriptable Object", EditorStyles.foldoutHeader);
                        EditorGUILayout.Space();

                        ScriptableObjectFolder = (DefaultAsset) EditorGUILayout.ObjectField("Scriptable Object Folder",
                            ScriptableObjectFolder, typeof(DefaultAsset),
                            false);
                        if (ScriptableObjectFolder != null)
                            EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(ScriptableObjectFolder));
                        createFoldForThisCollection =
                            EditorGUILayout.ToggleLeft($"Create parent {collectionName} folder",
                                createFoldForThisCollection);
                    }

                    using (new EditorGUILayout.VerticalScope("Box"))
                    {
                        EditorGUILayout.LabelField("Script", EditorStyles.foldoutHeader);
                        EditorGUILayout.Space();

                        ScriptsFolder = (DefaultAsset) EditorGUILayout.ObjectField("Script Folder", ScriptsFolder,
                            typeof(DefaultAsset),
                            false);
                        if (ScriptsFolder != null)
                        {
                            EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(ScriptsFolder));
                        }

                        createFoldForThisCollectionScripts =
                            EditorGUILayout.ToggleLeft($"Create parent {collectionName} folder",
                                createFoldForThisCollectionScripts);

                        TargetNameSpace = EditorGUILayout.TextField("Namespace", TargetNameSpace);
                    }

                    using (new EditorGUI.DisabledScope(!AreSettingsValid()))
                    {
                        Color color = GUI.color;
                        GUI.color = Color.green;
                        if (GUILayout.Button("Create"))
                        {
                            SaveLastUsed();
                            CreateNewCollection();
                        }

                        GUI.color = color;
                    }
                }
            }
        }

        private void SaveLastUsed()
        {
            CollectionUtility.ScriptsFolderPath = AssetDatabase.GetAssetPath(ScriptsFolder);
            CollectionUtility.TargetNamespace = TargetNameSpace;
            CollectionUtility.ScriptableObjectFolderPath = AssetDatabase.GetAssetPath(ScriptableObjectFolder);
        }

        private void CreateNewCollection()
        {
            bool scriptsGenerated = false;
            scriptsGenerated |= CreateCollectableScript();
            scriptsGenerated |= CreateCollectionScript();

            LastCollectionScriptableObjectPath = CreateCollectionObject();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!scriptsGenerated)
                OnAfterScriptsReloading();
        }

        private string CreateCollectionObject()
        {
            ScriptableObjectCollection targetCollection = CreateInstance<ScriptableObjectCollection>();
            targetCollection.name = collectionName;

            string targetFolderPath = AssetDatabase.GetAssetPath(ScriptableObjectFolder);
            if (createFoldForThisCollection)
                targetFolderPath = Path.Combine(targetFolderPath, $"{collectionName}");
            
            AssetDatabaseUtils.CreatePathIfDontExist(Path.Combine(targetFolderPath, "Items"));

            string collectionAssetPath = Path.Combine(targetFolderPath, $"{collectionName}.asset");
            AssetDatabase.CreateAsset(targetCollection, collectionAssetPath);
            return collectionAssetPath;
        }

        private bool CreateCollectableScript()
        {
            string targetFolder = AssetDatabase.GetAssetPath(ScriptsFolder);
            if (createFoldForThisCollectionScripts)
                targetFolder = Path.Combine(targetFolder, $"{collectionName}");
            
            return CodeGenerationUtility.CreateNewEmptyScript(collectableName, 
                targetFolder,
                TargetNameSpace, 
                $"public partial class {collectableName} : CollectableScriptableObject", 
                    typeof(CollectableScriptableObject));
        }
        
        private bool CreateCollectionScript()
        {
            string targetFolder = AssetDatabase.GetAssetPath(ScriptsFolder);
            if (createFoldForThisCollectionScripts)
                targetFolder = Path.Combine(targetFolder, $"{collectionName}");
            
            bool result = CodeGenerationUtility.CreateNewEmptyScript(collectionName, 
                targetFolder,
                TargetNameSpace, 
                $"public class {collectionName} : ScriptableObjectCollection<{collectableName}>", 
                typeof(ScriptableObjectCollection));

            if (string.IsNullOrEmpty(TargetNameSpace))
                LastCollectionFullName = $"{collectionName}";
            else
                LastCollectionFullName = $"{TargetNameSpace}.{collectionName}";

            LastCollectionScriptPath = Path.Combine(targetFolder, $"{collectionName}.cs");
            return result;
        }

        private bool AreSettingsValid()
        {
            if (string.IsNullOrEmpty(collectableName))
                return false;

            if (string.IsNullOrEmpty(collectionName))
                return false;

            if (ScriptsFolder == null)
                return false;

            if (ScriptableObjectFolder == null)
                return false;

            return true;
        }

        [DidReloadScripts]
        static void OnAfterScriptsReloading()
        {
            if (string.IsNullOrEmpty(LastCollectionScriptableObjectPath))
                return;

            ScriptableObjectCollection collectionAsset =
                AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(LastCollectionScriptableObjectPath);

            string assemblyName = CompilationPipeline.GetAssemblyNameFromScriptPath(LastCollectionScriptPath);

            Type targetType = Type.GetType($"{LastCollectionFullName}, {assemblyName}");
            
            ScriptableObject tempScriptable = CreateInstance(targetType);
            SerializedObject typeSerializedObj = new SerializedObject(tempScriptable);
            int typeId = typeSerializedObj.FindProperty("m_Script").objectReferenceInstanceIDValue;
            
            SerializedObject collectionScriptableObject = new SerializedObject(collectionAsset);
            collectionScriptableObject.FindProperty("m_Script").objectReferenceInstanceIDValue = typeId;
            collectionScriptableObject.ApplyModifiedProperties();

            LastCollectionScriptableObjectPath = string.Empty;
            LastCollectionFullName = string.Empty;

            Selection.objects = new[] {collectionScriptableObject.targetObject};
            EditorGUIUtility.PingObject(collectionScriptableObject.targetObject);
            GetWindowInstance().Close();
        }
    }
}
