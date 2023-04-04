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
        [Flags]
        private enum Fields
        {
            None = 0,
            ItemName = 1 << 0,
            CollectionName = 1 << 1,
            ScriptsFolder = 1 << 2,
        }

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
        private const string AUTO_COLLECTION_NAME_KEY = "AutoCollectionName";
        private const string COLLECTION_FORMAT_KEY = "CollectionFormat";
        private const string SCRIPT_FOLDER_MIRRORS_SCRIPTABLE_OBJECT_FOLDER_KEY =
            "ScriptFolderMirrorsScriptableObjectFolder";
        private const string CUSTOM_NAMESPACE_KEY = "CustomNamespace";
        private const string AUTOMATIC_NAMESPACE_BASED_ON_FOLDER_KEY = "AutomaticNamespaceBasedOnFolder";
        
        private const string ITEM_NAME_CONTROL = "CreateCollectionWizardItemName";
        private const string ITEM_NAME_DEFAULT = "Item";
        private const string COLLECTION_NAME_DEFAULT = "Collection";
        private const string COLLECTION_FORMAT_DEFAULT = "{0}" + COLLECTION_NAME_DEFAULT;
        private const string NAMESPACE_DEFAULT = "ScriptableObjects";

        private const string SCRIPTS_FOLDER_NAME = "Scripts";

        private static CreateCollectionWizard windowInstance;
        private static string targetFolder;

        private static readonly List<string> ScriptableObjectFolderNames = new List<string>
        {
            "ScriptableObject", "ScriptableObjects", "ScriptableObjectCollection", "ScriptableObjectCollections",
            "Database", "Databases", "Configuration", "Configurations", "Config", "Configs"
        };


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
        
        private string ScriptableObjectFolderPathWithoutParentFolder =>
            ScriptableObjectFolderBase == null
                ? "Assets/"
                : AssetDatabase.GetAssetPath(ScriptableObjectFolderBase);

        private string ScriptableObjectFolderPath
        {
            get
            {
                string folder = ScriptableObjectFolderPathWithoutParentFolder;
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
        
        private string ScriptsFolderPathWithoutParentFolder
        {
            get
            {
                if (ScriptFolderMirrorsScriptableObjectFolder.Value)
                    return AutomaticScriptFolder;
                
                return ScriptsFolderBase == null
                    ? "Assets/"
                    : AssetDatabase.GetAssetPath(ScriptsFolderBase);
            }
        }

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

        private string cachedAutomaticScriptFolder;
        private bool hasValidAutomaticScriptFolder;

        private string AutomaticScriptFolder
        {
            get
            {
                if (!hasValidAutomaticScriptFolder)
                {
                    string pathToInferFrom = ScriptableObjectFolderPathWithoutParentFolder;
                    cachedAutomaticScriptFolder = InferScriptFolderFromScriptableObjectFolder(pathToInferFrom);
                    hasValidAutomaticScriptFolder = true;
                }

                return cachedAutomaticScriptFolder;
            }
        }

        private string cachedNamespacePrefix;
        private string NamespacePrefix
        {
            get
            {
                if (string.IsNullOrEmpty(cachedNamespacePrefix))
                    cachedNamespacePrefix = ScriptableObjectCollectionSettings.GetInstance().NamespacePrefix;
                return cachedNamespacePrefix;
            }
            set
            {
                if (cachedNamespacePrefix == value)
                    return;
                
                cachedNamespacePrefix = value;
                ScriptableObjectCollectionSettings.GetInstance().SetNamespacePrefix(cachedNamespacePrefix);
            }
        }

        private string Namespace
        {
            get
            {
                string @namespace = NamespacePrefix;
                if (!string.IsNullOrEmpty(@namespace))
                    @namespace += NamespaceUtility.Separator;
                @namespace += NamespaceSuffix;
                return @namespace;
            }
        }
        
        private string cachedAutomaticNamespace;
        private bool hasValidAutomaticNamespace;

        private string AutomaticNamespace
        {
            get
            {
                if (!hasValidAutomaticNamespace)
                {
                    string pathToInferFrom = ScriptsFolderPathWithoutParentFolder;
                    int maxDepth = UseMaximumNamespaceDepth ? MaximumNamespaceDepth : int.MaxValue;
                    cachedAutomaticNamespace = NamespaceUtility.GetNamespaceForPath(pathToInferFrom, maxDepth);
                    hasValidAutomaticNamespace = true;
                }

                return cachedAutomaticNamespace;
            }
        }

        private string NamespaceSuffix =>
            AutomaticNamespaceBasedOnFolder.Value ? AutomaticNamespace : CustomNamespace.Value;
        
        private bool UseMaximumNamespaceDepth
        {
            get => ScriptableObjectCollectionSettings.GetInstance().UseMaximumNamespaceDepth;
            set
            {
                if (UseMaximumNamespaceDepth == value)
                    return;
                
                ScriptableObjectCollectionSettings.GetInstance().SetUseMaximumNamespaceDepth(value);
            }
        }
        
        private int MaximumNamespaceDepth
        {
            get => ScriptableObjectCollectionSettings.GetInstance().MaximumNamespaceDepth;
            set
            {
                if (MaximumNamespaceDepth == value)
                    return;
                
                ScriptableObjectCollectionSettings.GetInstance().SetMaximumNamespaceDepth(value);
            }
        }

        private static readonly EditorPreferenceBool FoldoutSettings =
            new EditorPreferenceBool(FOLDOUT_SETTINGS_KEY, true);
        private static readonly EditorPreferenceBool FoldoutScriptableObject =
            new EditorPreferenceBool(FOLDOUT_SCRIPTABLE_OBJECT_KEY, false, true);
        private static readonly EditorPreferenceBool FoldoutScript = new EditorPreferenceBool(FOLDOUT_SCRIPT_KEY, false, true);
        

        private static readonly EditorPreferenceBool WaitingRecompileForContinue =
            new EditorPreferenceBool(WAITING_SCRIPTS_TO_RECOMPILE_TO_CONTINUE_KEY, false, true);

        private static readonly EditorPreferenceString LastCollectionScriptableObjectPath =
            new EditorPreferenceString(LAST_COLLECTION_SCRIPTABLE_OBJECT_PATH_KEY, null, true);

        private static readonly EditorPreferenceString LastCollectionFullName =
            new EditorPreferenceString(LAST_COLLECTION_FULL_NAME_KEY, null, true);

        private static readonly EditorPreferenceString LastScriptsTargetFolder =
            new EditorPreferenceString(LAST_TARGET_SCRIPTS_FOLDER_KEY, null, true);

        private static readonly EditorPreferenceString LastGeneratedCollectionScriptPath =
            new EditorPreferenceString(LAST_GENERATED_COLLECTION_SCRIPT_PATH_KEY, null, true);

        private static readonly EditorPreferenceBool CreateFolderForThisCollection =
            new EditorPreferenceBool(CREATE_FOLDER_FOR_THIS_COLLECTION_KEY, false, true);

        private static readonly EditorPreferenceBool CreateFolderForThisCollectionScripts =
            new EditorPreferenceBool(CREATE_FOLDER_FOR_THIS_COLLECTION_SCRIPTS_KEY, false, true);
        
        private static readonly EditorPreferenceString CollectionFormat =
            new EditorPreferenceString(COLLECTION_FORMAT_KEY, COLLECTION_FORMAT_DEFAULT, true);

        private string cachedCollectionName = COLLECTION_NAME_DEFAULT;
        private string CollectionName
        {
            get
            {
                if (AutoCollectionName.Value)
                    return string.Format(CollectionFormat.Value, collectionItemName);
                return cachedCollectionName;
            }
            set => cachedCollectionName = value;
        }

        private string collectionItemName = ITEM_NAME_DEFAULT;

        private static readonly EditorPreferenceBool GenerateIndirectAccess =
            new EditorPreferenceBool(GENERATE_INDIRECT_ACCESS_KEY, true);
        
        private static readonly EditorPreferenceBool AutoCollectionName =
            new EditorPreferenceBool(AUTO_COLLECTION_NAME_KEY, true);
        
        private static readonly EditorPreferenceBool ScriptFolderMirrorsScriptableObjectFolder =
            new EditorPreferenceBool(SCRIPT_FOLDER_MIRRORS_SCRIPTABLE_OBJECT_FOLDER_KEY, true);

        private static readonly EditorPreferenceString CustomNamespace =
            new EditorPreferenceString(CUSTOM_NAMESPACE_KEY, NAMESPACE_DEFAULT);
        
        private static readonly EditorPreferenceBool AutomaticNamespaceBasedOnFolder = new EditorPreferenceBool(
            AUTOMATIC_NAMESPACE_BASED_ON_FOLDER_KEY, true);

        private Vector2 scrollPosition;
        [NonSerialized] 
        private bool didFocusDefaultControl;

        private string warningText;
        private Fields fieldsFilledInIncorrectly;

        [MenuItem("Assets/Create/ScriptableObject Collection/New Collection", false, 100)]
        private static void CreateNewItem()
        {
            string targetPath = "";
            if (Selection.objects.FirstOrDefault() is DefaultAsset folder)
                targetPath = AssetDatabase.GetAssetPath(folder);
            
            Show(targetPath);
        }
        
        private static CreateCollectionWizard GetWindowInstance()
        {
            if (windowInstance == null)
            {
                windowInstance =  CreateInstance<CreateCollectionWizard>();
                windowInstance.titleContent = new GUIContent("Create New Collection");
                windowInstance.minSize = new Vector2(EditorGUIUtility.labelWidth + EditorGUIUtility.labelWidth + 75, 370);
            }

            return windowInstance;
        }

        private void OnEnable()
        {
            windowInstance = this;

            if (string.IsNullOrEmpty(NamespacePrefix))
                NamespacePrefix = Application.companyName;
        }

        public static void Show(string targetPath)
        {
            targetFolder = targetPath;
            GetWindowInstance().ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.HelpBox(
                "The recommended workflow is to open the wizard on the folder in which you want to create the " +
                "Scriptable Object Collection, then fill in an Item Name and leave everything else to the defaults." +
                "\n\nThat will create a collection with an appropriate suffix, and scripts will be created in a folder " +
                "that mirrors the folder of the Collection." +
                "\n\nFirst-time users should specify a namespace prefix in the Script section.",
                MessageType.Info);

            DrawSections();
            
            EditorGUILayout.EndScrollView();
            bool didChange = EditorGUI.EndChangeCheck();
            if (didChange)
            {
                hasValidAutomaticScriptFolder = false;
                hasValidAutomaticNamespace = false;
            }
            
            EditorGUILayout.Space();

            bool areSettingsInvalid = !CheckValidityOfSettings();

            if (areSettingsInvalid)
                EditorGUILayout.HelpBox(warningText, MessageType.Warning);

            using (new EditorGUI.DisabledScope(areSettingsInvalid))
            {
                if (GUILayout.Button("Create", GUILayout.Height(35)))
                    CreateNewCollection();
            }

            if (!didFocusDefaultControl)
            {
                EditorGUI.FocusTextInControl(ITEM_NAME_CONTROL);
                didFocusDefaultControl = true;
            }
        }

        private void DrawSections()
        {
            DrawSettingsSection();

            DrawScriptableObjectSection();

            DrawScriptsSection();
        }

        private void SetColorBasedOnFieldValidity(Fields field)
        {
            GUI.color = (fieldsFilledInIncorrectly & field) == field ? Color.yellow : Color.white;
        }
        
        private void ResetColorBasedOnFieldValidity()
        {
            GUI.color = Color.white;
        }

        private void DrawSettingsSection()
        {
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                FoldoutSettings.Value = EditorGUILayout.BeginFoldoutHeaderGroup(FoldoutSettings.Value, "Settings");

                EditorGUI.indentLevel++;
                if (FoldoutSettings.Value)
                {
                    GUI.SetNextControlName(ITEM_NAME_CONTROL);
                    SetColorBasedOnFieldValidity(Fields.ItemName);
                    collectionItemName = EditorGUILayout.TextField("Item Name", collectionItemName);
                    ResetColorBasedOnFieldValidity();

                    EditorGUILayout.Space();
                    
                    // Allow the collection name to be entered manually for this particular collection, or be automatic.
                    SetColorBasedOnFieldValidity(Fields.CollectionName);
                    if (AutoCollectionName.Value)
                        EditorGUILayout.LabelField("Collection Name", CollectionName);
                    else
                        CollectionName = EditorGUILayout.TextField("Collection Name", CollectionName);
                    ResetColorBasedOnFieldValidity();

                    // Some basic controls for how the collection name is generated.
                    EditorGUILayout.BeginHorizontal();
                    AutoCollectionName.DrawGUILayout(
                        "Auto Collection Name", GUILayout.Width(EditorGUIUtility.labelWidth + 16));
                    bool wasGuiEnabled = GUI.enabled;
                    GUI.enabled = AutoCollectionName.Value;
                    string collectionFormatControlName = "CreateCollectionWizardCollectionFormat";
                    GUI.SetNextControlName(collectionFormatControlName);
                    CollectionFormat.DrawGUILayout(GUIContent.none, GUILayout.MinWidth(50));
                    
                    // Allow the format to be reset.
                    bool reset = GUILayout.Button("R", EditorStyles.miniButton, GUILayout.Width(24));
                    if (reset)
                    {
                        CollectionFormat.Value = COLLECTION_FORMAT_DEFAULT;
                        
                        // Make sure to deselect the collection format otherwise you don't see it reset.
                        if (GUI.GetNameOfFocusedControl() == collectionFormatControlName)
                            GUI.FocusControl(string.Empty);
                    }
                    GUI.enabled = wasGuiEnabled;
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();

                    GenerateIndirectAccess.DrawGUILayout();
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUI.indentLevel--;
            }
        }

        private void DrawScriptableObjectSection()
        {
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
                    CreateFolderForThisCollection.DrawGUILayoutLeft($"Create parent {CollectionName} folder");
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawScriptsSection()
        {
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                FoldoutScript.Value = EditorGUILayout.BeginFoldoutHeaderGroup(FoldoutScript.Value, "Script");
                EditorGUI.indentLevel++;
                if (FoldoutScript.Value)
                {
                    EditorGUILayout.LabelField(ScriptsFolderPath, EditorStyles.miniLabel);

                    SetColorBasedOnFieldValidity(Fields.ScriptsFolder);
                    if (!ScriptFolderMirrorsScriptableObjectFolder.Value)
                    {
                        ScriptsFolderBase = (DefaultAsset)EditorGUILayout.ObjectField(
                            "Base Folder", ScriptsFolderBase,
                            typeof(DefaultAsset),
                            false);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Base Folder", AutomaticScriptFolder);
                    }
                    ResetColorBasedOnFieldValidity();

                    ScriptFolderMirrorsScriptableObjectFolder.DrawGUILayoutLeft("Script Folder Mirrors Scriptable Object Folder");

                    CreateFolderForThisCollectionScripts.DrawGUILayoutLeft($"Create parent {CollectionName} folder");

                    EditorGUILayout.Space();
                    
                    EditorGUILayout.LabelField("Namespace", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(Namespace, EditorStyles.miniLabel);

                    // Draw fields for the individual components of the namespace.
                    EditorGUILayout.BeginHorizontal();
                    NamespacePrefix = EditorGUILayout.TextField(
                        NamespacePrefix, GUILayout.Width(EditorGUIUtility.labelWidth));
                    if (AutomaticNamespaceBasedOnFolder.Value)
                        EditorGUILayout.LabelField(AutomaticNamespace, GUILayout.MinWidth(30));
                    else
                        CustomNamespace.DrawGUILayout(GUIContent.none, GUILayout.MinWidth(30));
                    EditorGUILayout.EndHorizontal();

                    // Draw a checkbox to make the namespace be inferred from the script folder, or specified manually.
                    EditorGUILayout.BeginHorizontal();
                    AutomaticNamespaceBasedOnFolder.DrawGUILayoutLeft("Automatic Based On Folder");
                    EditorGUILayout.EndHorizontal();

                    // You can also specify if it should be clamped to a certain depth.
                    bool wasGuiEnabled = GUI.enabled;
                    GUI.enabled = AutomaticNamespaceBasedOnFolder.Value;
                    {
                        EditorGUILayout.BeginHorizontal();
                        UseMaximumNamespaceDepth = EditorGUILayout.ToggleLeft(
                            "Max. Depth", UseMaximumNamespaceDepth, GUILayout.Width(EditorGUIUtility.labelWidth));
                        GUI.enabled = GUI.enabled && UseMaximumNamespaceDepth;
                        MaximumNamespaceDepth = EditorGUILayout.IntField(
                            GUIContent.none, MaximumNamespaceDepth, GUILayout.MinWidth(30));
                        EditorGUILayout.EndHorizontal();
                    }
                    GUI.enabled = wasGuiEnabled;
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUI.indentLevel--;
            }
        }

        private string InferScriptFolderFromScriptableObjectFolder(string pathToInferFrom)
        {
            pathToInferFrom = pathToInferFrom.ToPathWithConsistentSeparators();
            string[] folders = pathToInferFrom.Split(Path.AltDirectorySeparatorChar);
            bool didFindScriptableObjectsFolder = false;
            
            string scriptsFolder = string.Empty;
            for (int i = 0; i < folders.Length; i++)
            {
                if (i > 0)
                    scriptsFolder += Path.AltDirectorySeparatorChar;
                
                // The Scriptable Objects folder is expected to have a certain name. That particular name is to be
                // replaced with the name of the scripts folder so that we create a folder that is on the same level as
                // the configurations, but inside a Scripts folder instead.
                if (!didFindScriptableObjectsFolder && ScriptableObjectFolderNames.Contains(folders[i]))
                {
                    scriptsFolder += SCRIPTS_FOLDER_NAME;
                    didFindScriptableObjectsFolder = true;
                    continue;
                }
                
                scriptsFolder += folders[i];
            }

            return scriptsFolder;
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

            AssetDatabaseUtils.CreatePathIfDoesntExist(folderPath);
            using (StreamWriter writer = new StreamWriter(Path.Combine(folderPath, $"{fileName}.cs")))
            {
                int indentation = 0;
                List<string> directives = new List<string>();
                directives.Add(typeof(ScriptableObjectCollection).Namespace);
                directives.Add(Namespace);
                directives.Add("System");
                directives.Add("UnityEngine");

                CodeGenerationUtility.AppendHeader(writer, ref indentation, Namespace, "[Serializable]",
                    $"public sealed class {collectionItemName}IndirectReference : CollectionItemIndirectReference<{collectionItemName}>",
                    directives.Distinct().ToArray());

                CodeGenerationUtility.AppendLine(writer, indentation,
                    $"public {collectionItemName}IndirectReference() {{}}");
                
                CodeGenerationUtility.AppendLine(writer, indentation,
                    $"public {collectionItemName}IndirectReference({collectionItemName} collectionItemScriptableObject) : base(collectionItemScriptableObject) {{}}");

                indentation--;
                CodeGenerationUtility.AppendFooter(writer, ref indentation, Namespace);
            }
        }

        private bool CreateCollectionItemScript()
        {
            string folder = ScriptsFolderPath;
            LastScriptsTargetFolder.Value = ScriptsFolderPathWithoutParentFolder;

            return CodeGenerationUtility.CreateNewEmptyScript(collectionItemName, 
                folder,
                Namespace, 
                string.Empty,
                $"public partial class {collectionItemName} : {nameof(ScriptableObjectCollectionItem)}", null, 
                    typeof(ScriptableObjectCollectionItem).Namespace);
        }
        
        private bool CreateCollectionScript()
        {
            string folder = ScriptsFolderPath;

            bool result = CodeGenerationUtility.CreateNewEmptyScript(CollectionName,
                folder,
                Namespace,
                $"[CreateAssetMenu(menuName = \"ScriptableObject Collection/Collections/Create {CollectionName}\", fileName = \"{CollectionName}\", order = 0)]",
                $"public class {CollectionName} : ScriptableObjectCollection<{collectionItemName}>",
                null,
                typeof(ScriptableObjectCollection).Namespace, "UnityEngine", "System.Collections.Generic");

            if (string.IsNullOrEmpty(Namespace))
                LastCollectionFullName.Value = $"{CollectionName}";
            else
                LastCollectionFullName.Value = $"{Namespace}.{CollectionName}";

            LastGeneratedCollectionScriptPath.Value = Path.Combine(folder, $"{CollectionName}.cs");
            return result;
        }

        private bool CheckValidityOfSettings()
        {
            warningText = string.Empty;
            fieldsFilledInIncorrectly = Fields.None;
            bool isValid = true;
            
            if (string.IsNullOrEmpty(collectionItemName))
            {
                isValid = false;
                fieldsFilledInIncorrectly |= Fields.ItemName;
                warningText += "Item Name shouldn't be empty.\n";
            }

            if (string.IsNullOrEmpty(CollectionName))
            {
                isValid = false;
                fieldsFilledInIncorrectly |= Fields.CollectionName;
                warningText += "Collection Name shouldn't be empty.\n";
            }

            if (collectionItemName == CollectionName)
            {
                isValid = false;
                fieldsFilledInIncorrectly |=
                    Fields.ItemName | Fields.CollectionName;
                warningText += "Item Name shouldn't be the same as the Collection Name.\n";
            }

            if (ScriptsFolderBase == null)
            {
                isValid = false;
                fieldsFilledInIncorrectly |= Fields.ScriptsFolder;
                warningText += "Script folder isn't valid.\n";
            }

            return isValid;
        }

        [DidReloadScripts]
        static void OnAfterScriptsReloading()
        {
            if (!WaitingRecompileForContinue.Value)
                return;

            WaitingRecompileForContinue.Value = false;

            string assemblyName =
                CompilationPipeline.GetAssemblyNameFromScriptPath(LastGeneratedCollectionScriptPath.Value);

            Type targetType = Type.GetType($"{LastCollectionFullName.Value}, {assemblyName}");
            
            ScriptableObjectCollection collectionAsset =
                ScriptableObjectCollectionUtility.CreateScriptableObjectOfType(targetType, 
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
