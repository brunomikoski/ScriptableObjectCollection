using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public sealed class CreateCollectionWizard : EditorWindow
    {
        private const string FOLDOUT_SETTINGS_KEY = "FoldoutSettings";
        private const string FOLDOUT_SCRIPTABLE_OBJECT_KEY = "FoldoutScriptableObject";
        private const string FOLDOUT_SCRIPT_KEY = "FoldoutScript";
        
        private const string WAITING_SCRIPTS_TO_RECOMPILE_TO_CONTINUE_KEY = "WaitingScriptsToRecompileToContinueKey";
        private const string LAST_COLLECTION_SCRIPTABLE_OBJECT_PATH_KEY = "CollectionScriptableObjectPathKey";
        private const string LAST_COLLECTION_FULL_NAME_KEY = "CollectionFullNameKey";
        private const string LAST_GENERATED_COLLECTION_SCRIPT_PATH_KEY = "CollectionScriptPathKey";
        private const string LAST_TARGET_SCRIPTS_FOLDER_KEY = "LastTargetScriptsFolder";
        private const string GENERATE_INDIRECT_ACCESS_KEY = "GenerateIndirectAccess";
        private const string CREATE_FOLDER_FOR_THIS_COLLECTION_KEY = "CreateFolderForThisCollection";
        private const string CREATE_FOLDER_FOR_THIS_COLLECTION_SCRIPTS_KEY = "CreateFolderForThisCollectionScripts";
        private const string INFER_COLLECTION_NAME_KEY = "InferCollectionName";
        private const string COLLECTION_SUFFIX_KEY = "CollectionSuffix";
        
        private const string COLLECTION_NAME_DEFAULT = "Collection";
        
        private static CreateCollectionWizard windowInstance;
        private static string targetFolder;


        private DefaultAsset cachedScriptableObjectFolderBase;
        private DefaultAsset ScriptableObjectFolderBase
        {
            get
            {
                if (cachedScriptableObjectFolderBase != null) 
                    return cachedScriptableObjectFolderBase;

                if (!string.IsNullOrEmpty(targetFolder))
                {
                    cachedScriptableObjectFolderBase = AssetDatabase.LoadAssetAtPath<DefaultAsset>(targetFolder);
                    return cachedScriptableObjectFolderBase;
                }

                if (!string.IsNullOrEmpty(LastCollectionScriptableObjectPath.Value))
                {
                    cachedScriptableObjectFolderBase =
                        AssetDatabase.LoadAssetAtPath<DefaultAsset>(LastCollectionScriptableObjectPath.Value);
                    return cachedScriptableObjectFolderBase;
                }
                
                return cachedScriptableObjectFolderBase;
            }
            set => cachedScriptableObjectFolderBase = value;
        }
        
        private string ScriptableObjectFolderPath
        {
            get
            {
                string folder = ScriptableObjectFolderBase == null
                    ? "Assets/"
                    : AssetDatabase.GetAssetPath(ScriptableObjectFolderBase);
                if (CreateFolderForThisCollection.Value)
                    folder = Path.Combine(folder, $"{CollectionName}");
                return folder.ToPathWithConsistentSeparators();
            }
        }

        private DefaultAsset cachedScriptsFolderBase;
        private DefaultAsset ScriptsFolderBase
        {
            get
            {
                if (cachedScriptsFolderBase != null) 
                    return cachedScriptsFolderBase;
                
                if (!string.IsNullOrEmpty(LastScriptsTargetFolder.Value))
                {
                    cachedScriptsFolderBase =
                        AssetDatabase.LoadAssetAtPath<DefaultAsset>(
                            Path.GetDirectoryName(LastScriptsTargetFolder.Value));
                    return cachedScriptsFolderBase;
                }
                
                if (!string.IsNullOrEmpty(targetFolder))
                {
                    cachedScriptsFolderBase = AssetDatabase.LoadAssetAtPath<DefaultAsset>(targetFolder);
                    return cachedScriptsFolderBase;
                }
                
                return cachedScriptsFolderBase;
            }
            set => cachedScriptsFolderBase = value;
        }
        
        private string ScriptsFolderPathWithoutParentFolder =>
            ScriptsFolderBase == null
                ? "Assets/"
                : AssetDatabase.GetAssetPath(ScriptsFolderBase);

        private string ScriptsFolderPath
        {
            get
            {
                string folder = ScriptsFolderPathWithoutParentFolder;
                if (CreateFolderForThisCollectionScripts.Value)
                    folder = Path.Combine(folder, $"{CollectionName}");
                return folder.ToPathWithConsistentSeparators();
            }
        }

        private string cachedNameSpace;
        private string TargetNameSpace
        {
            get
            {
                if (string.IsNullOrEmpty(cachedNameSpace))
                    cachedNameSpace = ScriptableObjectCollectionSettings.GetInstance().DefaultNamespace;
                return cachedNameSpace;
            }
            set
            {
                cachedNameSpace = value;
                if (string.IsNullOrEmpty(ScriptableObjectCollectionSettings.GetInstance().DefaultNamespace))
                    ScriptableObjectCollectionSettings.GetInstance().SetDefaultNamespace(cachedNameSpace);
            }
        }

        private static readonly EditorPreferenceBool FoldoutSettings =
            new EditorPreferenceBool(FOLDOUT_SETTINGS_KEY, true);
        private static readonly EditorPreferenceBool FoldoutScriptableObject =
            new EditorPreferenceBool(FOLDOUT_SCRIPTABLE_OBJECT_KEY, true);
        private static readonly EditorPreferenceBool FoldoutScript = new EditorPreferenceBool(FOLDOUT_SCRIPT_KEY, true);
        

        private static readonly EditorPreferenceBool WaitingRecompileForContinue =
            new EditorPreferenceBool(WAITING_SCRIPTS_TO_RECOMPILE_TO_CONTINUE_KEY);

        private static readonly EditorPreferenceString LastCollectionScriptableObjectPath =
            new EditorPreferenceString(LAST_COLLECTION_SCRIPTABLE_OBJECT_PATH_KEY);

        private static readonly EditorPreferenceString LastCollectionFullName =
            new EditorPreferenceString(LAST_COLLECTION_FULL_NAME_KEY);

        private static readonly EditorPreferenceString LastScriptsTargetFolder =
            new EditorPreferenceString(LAST_TARGET_SCRIPTS_FOLDER_KEY);

        private static readonly EditorPreferenceString LastGeneratedCollectionScriptPath =
            new EditorPreferenceString(LAST_GENERATED_COLLECTION_SCRIPT_PATH_KEY);

        private static readonly EditorPreferenceBool CreateFolderForThisCollection =
            new EditorPreferenceBool(CREATE_FOLDER_FOR_THIS_COLLECTION_KEY, true);

        private static readonly EditorPreferenceBool CreateFolderForThisCollectionScripts =
            new EditorPreferenceBool(CREATE_FOLDER_FOR_THIS_COLLECTION_SCRIPTS_KEY, true);
        
        private static readonly EditorPreferenceString CollectionSuffix =
            new EditorPreferenceString(COLLECTION_SUFFIX_KEY, COLLECTION_NAME_DEFAULT);

        private string cachedCollectionName = COLLECTION_NAME_DEFAULT;
        private string CollectionName
        {
            get
            {
                if (InferCollectionName.Value)
                    return collectionItemName + CollectionSuffix.Value;
                return cachedCollectionName;
            }
            set => cachedCollectionName = value;
        }

        private string collectionItemName = "Item";

        private static readonly EditorPreferenceBool GenerateIndirectAccess =
            new EditorPreferenceBool(GENERATE_INDIRECT_ACCESS_KEY, true);
        
        private static readonly EditorPreferenceBool InferCollectionName =
            new EditorPreferenceBool(INFER_COLLECTION_NAME_KEY, true);

        private static CreateCollectionWizard GetWindowInstance()
        {
            if (windowInstance == null)
            {
                windowInstance =  CreateInstance<CreateCollectionWizard>();
                windowInstance.titleContent = new GUIContent("Create New Collection");
            }

            return windowInstance;
        }

        private void OnEnable()
        {
            windowInstance = this;
        }

        public static void Show(string targetPath)
        {
            targetFolder = targetPath;
            GetWindowInstance().ShowUtility();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                FoldoutSettings.Value = EditorGUILayout.BeginFoldoutHeaderGroup(FoldoutSettings.Value, "Settings");

                EditorGUI.indentLevel++;
                if (FoldoutSettings.Value)
                {
                    collectionItemName = EditorGUILayout.TextField("Item Name", collectionItemName);

                    EditorGUILayout.Space();

                    InferCollectionName.DrawGUILayout();
                    if (InferCollectionName.Value)
                    {
                        EditorGUILayout.LabelField("Collection Name", CollectionName);
                        CollectionSuffix.DrawGUILayout();
                    }
                    else
                    {
                        CollectionName = EditorGUILayout.TextField("Collection Name", CollectionName);
                    }

                    EditorGUILayout.Space();

                    GenerateIndirectAccess.DrawGUILayout();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUI.indentLevel--;
            }

            using (new EditorGUILayout.VerticalScope("Box"))
            {
                FoldoutScriptableObject.Value = EditorGUILayout.BeginFoldoutHeaderGroup(
                    FoldoutScriptableObject.Value, "Scriptable Object");

                EditorGUI.indentLevel++;
                if (FoldoutScriptableObject.Value)
                {
                    EditorGUILayout.LabelField(ScriptableObjectFolderPath, EditorStyles.miniLabel);

                    ScriptableObjectFolderBase = (DefaultAsset)EditorGUILayout.ObjectField(
                        "Base Folder",
                        ScriptableObjectFolderBase, typeof(DefaultAsset),
                        false);
                    CreateFolderForThisCollection.Value = EditorGUILayout.ToggleLeft(
                        $"Create parent {CollectionName} folder", CreateFolderForThisCollection.Value);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUI.indentLevel--;
            }

            using (new EditorGUILayout.VerticalScope("Box"))
            {
                FoldoutScript.Value = EditorGUILayout.BeginFoldoutHeaderGroup(FoldoutScript.Value, "Script");
                EditorGUI.indentLevel++;
                if (FoldoutScript.Value)
                {
                    EditorGUILayout.LabelField(ScriptsFolderPath, EditorStyles.miniLabel);

                    ScriptsFolderBase = (DefaultAsset)EditorGUILayout.ObjectField(
                        "Base Folder", ScriptsFolderBase,
                        typeof(DefaultAsset),
                        false);

                    CreateFolderForThisCollectionScripts.Value =
                        EditorGUILayout.ToggleLeft(
                            $"Create parent {CollectionName} folder",
                            CreateFolderForThisCollectionScripts.Value);
                    
                    EditorGUILayout.Space();

                    TargetNameSpace = EditorGUILayout.TextField("Namespace", TargetNameSpace);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUI.indentLevel--;
            }

            using (new EditorGUI.DisabledScope(!AreSettingsValid()))
            {
                if (GUILayout.Button("Create", GUILayout.Height(35)))
                    CreateNewCollection();
            }
        }

        private void CreateNewCollection()
        {
            bool scriptsGenerated = false;
            scriptsGenerated |= CreateCollectionItemScript();
            scriptsGenerated |= CreateCollectionScript();

            if (GenerateIndirectAccess.Value)
                CreateIndirectAccess();
                
            WaitingRecompileForContinue.Value = true;
            
            AssetDatabase.Refresh();

            if (!scriptsGenerated)
                OnAfterScriptsReloading();
        }

        private void CreateIndirectAccess()
        {
            string folderPath = ScriptsFolderPath;

            string fileName = $"{collectionItemName}IndirectReference";

            AssetDatabaseUtils.CreatePathIfDontExist(folderPath);
            using (StreamWriter writer = new StreamWriter(Path.Combine(folderPath, $"{fileName}.cs")))
            {
                int indentation = 0;
                List<string> directives = new List<string>();
                directives.Add(typeof(ScriptableObjectCollectionItem).Namespace);
                directives.Add(TargetNameSpace);
                directives.Add("System");
                directives.Add("UnityEngine");

                CodeGenerationUtility.AppendHeader(writer, ref indentation, TargetNameSpace, "[Serializable]",
                    $"public sealed class {collectionItemName}IndirectReference : CollectionItemIndirectReference<{collectionItemName}>",
                    directives.Distinct().ToArray());

                CodeGenerationUtility.AppendLine(writer, indentation,
                    $"public {collectionItemName}IndirectReference() {{}}");
                
                CodeGenerationUtility.AppendLine(writer, indentation,
                    $"public {collectionItemName}IndirectReference({collectionItemName} collectionItemScriptableObject) : base(collectionItemScriptableObject) {{}}");

                indentation--;
                CodeGenerationUtility.AppendFooter(writer, ref indentation, TargetNameSpace);
            }
        }

        private bool CreateCollectionItemScript()
        {
            string folder = ScriptsFolderPath;
            LastScriptsTargetFolder.Value = ScriptsFolderPathWithoutParentFolder;

            return CodeGenerationUtility.CreateNewEmptyScript(collectionItemName, 
                folder,
                TargetNameSpace, 
                string.Empty,
                $"public partial class {collectionItemName} : ScriptableObjectCollectionItem", 
                    typeof(ScriptableObjectCollectionItem).Namespace);
        }
        
        private bool CreateCollectionScript()
        {
            string folder = ScriptsFolderPath;

            bool result = CodeGenerationUtility.CreateNewEmptyScript(CollectionName,
                folder,
                TargetNameSpace,
                $"[CreateAssetMenu(menuName = \"ScriptableObject Collection/Collections/Create {CollectionName}\", fileName = \"{CollectionName}\", order = 0)]",
                $"public class {CollectionName} : ScriptableObjectCollection<{collectionItemName}>", typeof(ScriptableObjectCollection).Namespace, "UnityEngine");

            if (string.IsNullOrEmpty(TargetNameSpace))
                LastCollectionFullName.Value = $"{CollectionName}";
            else
                LastCollectionFullName.Value = $"{TargetNameSpace}.{CollectionName}";

            LastGeneratedCollectionScriptPath.Value = Path.Combine(folder, $"{CollectionName}.cs");
            return result;
        }

        private bool AreSettingsValid()
        {
            if (string.IsNullOrEmpty(collectionItemName))
                return false;

            if (string.IsNullOrEmpty(CollectionName))
                return false;

            if (ScriptsFolderBase == null)
                return false;

            return true;
        }

        [DidReloadScripts]
        static void OnAfterScriptsReloading()
        {
            if (!WaitingRecompileForContinue.Value)
                return;

            WaitingRecompileForContinue.Value = false;

            string assemblyName =
                CompilationPipeline.GetAssemblyNameFromScriptPath(LastGeneratedCollectionScriptPath.Value);

            Type targetType = Type.GetType($"{LastCollectionFullName}, {assemblyName}");
            
            ScriptableObjectCollection collectionAsset =
                ScriptableObjectCollectionUtils.CreateScriptableObjectOfType(targetType, 
                    windowInstance.ScriptableObjectFolderPath, windowInstance.CollectionName) as ScriptableObjectCollection;
            
            Selection.objects = new Object[] {collectionAsset};
            EditorGUIUtility.PingObject(collectionAsset);

            CreateCollectionWizard openWindowInstance = GetWindow<CreateCollectionWizard>();
            openWindowInstance.Close();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
