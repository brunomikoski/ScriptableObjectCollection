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
        private const string WAITING_SCRIPTS_TO_RECOMPILE_TO_CONTINUE_KEY = "WaitingScriptsToRecompileToContinueKey";
        private const string LAST_COLLECTION_SCRIPTABLE_OBJECT_PATH_KEY = "CollectionScriptableObjectPathKey";
        private const string LAST_COLLECTION_FULL_NAME_KEY = "CollectionFullNameKey";
        private const string LAST_GENERATED_COLLECTION_SCRIPT_PATH_KEY = "CollectionScriptPathKey";
        private const string LAST_TARGET_SCRIPTS_FOLDER_KEY = "LastTargetScriptsFolder";

        private DefaultAsset cachedScriptableObjectFolder;
        private DefaultAsset ScriptableObjectFolder
        {
            get
            {
                if (cachedScriptableObjectFolder != null) 
                    return cachedScriptableObjectFolder;

                if (!string.IsNullOrEmpty(targetFolder))
                {
                    cachedScriptableObjectFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(targetFolder);
                    return cachedScriptableObjectFolder;
                }

                if (!string.IsNullOrEmpty(LastCollectionScriptableObjectPath))
                {
                    cachedScriptableObjectFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(LastCollectionScriptableObjectPath);
                    return cachedScriptableObjectFolder;
                }
                
                if (!string.IsNullOrEmpty(ScriptableObjectCollectionSettings.Instance.DefaultScriptableObjectsFolder))
                {
                    cachedScriptableObjectFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(
                        ScriptableObjectCollectionSettings.Instance.DefaultScriptableObjectsFolder);
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
                
                if (!string.IsNullOrEmpty(LastScriptsTargetFolder))
                {
                    cachedScriptsFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(Path.GetDirectoryName(LastScriptsTargetFolder));
                    return cachedScriptsFolder;
                }
                
                if (!string.IsNullOrEmpty(targetFolder))
                {
                    cachedScriptsFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(targetFolder);
                    return cachedScriptsFolder;
                }
                
                if (!string.IsNullOrEmpty(ScriptableObjectCollectionSettings.Instance.DefaultScriptsFolder))
                {
                    cachedScriptsFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(
                        ScriptableObjectCollectionSettings.Instance.DefaultScriptsFolder);
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
                    cachedNameSpace = ScriptableObjectCollectionSettings.Instance.DefaultNamespace;
                return cachedNameSpace;
            }
            set => cachedNameSpace = value;
        }


        private static bool WaitingRecompileForContinue
        {
            get => EditorPrefs.GetBool(WAITING_SCRIPTS_TO_RECOMPILE_TO_CONTINUE_KEY, false);
            set => EditorPrefs.SetBool(WAITING_SCRIPTS_TO_RECOMPILE_TO_CONTINUE_KEY, value);
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

        private static string LastScriptsTargetFolder
        {
            get => EditorPrefs.GetString(LAST_TARGET_SCRIPTS_FOLDER_KEY, String.Empty);
            set => EditorPrefs.SetString(LAST_TARGET_SCRIPTS_FOLDER_KEY, value);
        }
        
        private static string LastGeneratedCollectionScriptPath
        {
            get => EditorPrefs.GetString(LAST_GENERATED_COLLECTION_SCRIPT_PATH_KEY, String.Empty);
            set => EditorPrefs.SetString(LAST_GENERATED_COLLECTION_SCRIPT_PATH_KEY, value);
        }

        private bool createFoldForThisCollection = true;
        private bool createFoldForThisCollectionScripts = true;

        private string collectionName = "Collection";
        private string collectableName = "Collectable";
        private static string targetFolder;
        private bool generateIndirectAccess = true;

        public static CreateCollectionWizzard GetWindowInstance()
        {
            return GetWindow<CreateCollectionWizzard>("Creating New Collection");
        }
        
        public static void Show(string targetPath)
        {
            targetFolder = targetPath;
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

                        generateIndirectAccess = EditorGUILayout.Toggle("Generate Indirect Access", generateIndirectAccess);
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
                            CreateNewCollection();

                        GUI.color = color;
                    }
                }
            }
        }

        private void CreateNewCollection()
        {
            bool scriptsGenerated = false;
            scriptsGenerated |= CreateCollectableScript();
            scriptsGenerated |= CreateCollectionScript();

            if (generateIndirectAccess)
                CreateIndirectAccess();
            WaitingRecompileForContinue = true;

            LastCollectionScriptableObjectPath = CreateCollectionObject();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!scriptsGenerated)
                OnAfterScriptsReloading();
        }

        private void CreateIndirectAccess()
        {
            string folderPath = AssetDatabase.GetAssetPath(ScriptsFolder);
            if (createFoldForThisCollectionScripts)
                folderPath = Path.Combine(folderPath, $"{collectionName}");

            CodeGenerationUtility.CreateNewEmptyScript($"{collectableName}IndirectReference", 
                folderPath,
                TargetNameSpace, 
                string.Empty,
                $"public sealed class {collectableName}IndirectReference : CollectableIndirectReference<{collectableName}>", 
                new []{typeof(CollectableScriptableObject).Namespace, TargetNameSpace});
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
            string folder = AssetDatabase.GetAssetPath(ScriptsFolder);
            LastScriptsTargetFolder = folder;
            if (createFoldForThisCollectionScripts)
                folder = Path.Combine(folder, $"{collectionName}");
            
            return CodeGenerationUtility.CreateNewEmptyScript(collectableName, 
                folder,
                TargetNameSpace, 
                string.Empty,
                $"public partial class {collectableName} : CollectableScriptableObject", 
                    typeof(CollectableScriptableObject).Namespace);
        }
        
        private bool CreateCollectionScript()
        {
            string targetFolder = AssetDatabase.GetAssetPath(ScriptsFolder);
            if (createFoldForThisCollectionScripts)
                targetFolder = Path.Combine(targetFolder, $"{collectionName}");

            bool result = CodeGenerationUtility.CreateNewEmptyScript(collectionName,
                targetFolder,
                TargetNameSpace,
                $"[CreateAssetMenu(menuName = \"ScriptableObject Collection/Collections/Create {collectionName}\", fileName = \"{collectionName}\", order = 0)]",
                $"public class {collectionName} : ScriptableObjectCollection<{collectableName}>", typeof(ScriptableObjectCollection).Namespace, "UnityEngine");

            if (string.IsNullOrEmpty(TargetNameSpace))
                LastCollectionFullName = $"{collectionName}";
            else
                LastCollectionFullName = $"{TargetNameSpace}.{collectionName}";

            LastGeneratedCollectionScriptPath = Path.Combine(targetFolder, $"{collectionName}.cs");
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
            if (!WaitingRecompileForContinue)
                return;

            WaitingRecompileForContinue = false;

            ScriptableObjectCollection collectionAsset =
                AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(LastCollectionScriptableObjectPath);

            string assemblyName = CompilationPipeline.GetAssemblyNameFromScriptPath(LastGeneratedCollectionScriptPath);

            Type targetType = Type.GetType($"{LastCollectionFullName}, {assemblyName}");
            
            ScriptableObject tempScriptable = CreateInstance(targetType);
            SerializedObject typeSerializedObj = new SerializedObject(tempScriptable);
            int typeId = typeSerializedObj.FindProperty("m_Script").objectReferenceInstanceIDValue;
            
            SerializedObject collectionScriptableObject = new SerializedObject(collectionAsset);
            collectionScriptableObject.FindProperty("m_Script").objectReferenceInstanceIDValue = typeId;
            collectionScriptableObject.ApplyModifiedProperties();

            Selection.objects = new[] {collectionScriptableObject.targetObject};
            EditorGUIUtility.PingObject(collectionScriptableObject.targetObject);
            GetWindowInstance().Close();
        }
    }
}
