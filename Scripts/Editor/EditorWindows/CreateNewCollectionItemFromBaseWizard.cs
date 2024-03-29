using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public sealed class CreateNewCollectionItemFromBaseWizard : EditorWindow
    {
        private const string LAST_COLLECTION_ITEM_SCRIPT_PATH = "CollectionItemScriptPathKey";
        private const string LAST_COLLECTION_ITEM_FULL_NAME_KEY = "NewCollectionItemFullNameKey";

        
        private string newClassName;
        private DefaultAsset targetFolder;
        
        private static Type targetType;
        private static Action<bool> onCreationCallback;

        public static readonly EditorPreferenceString LastGeneratedCollectionScriptPath =
            new EditorPreferenceString(LAST_COLLECTION_ITEM_SCRIPT_PATH);

        public static readonly EditorPreferenceString LastCollectionFullName =
            new EditorPreferenceString(LAST_COLLECTION_ITEM_FULL_NAME_KEY);

        public static void Show(Type baseType, Action<bool> targetOnCreationCallback)
        {
            onCreationCallback = targetOnCreationCallback;
            targetType = baseType;
            CreateNewCollectionItemFromBaseWizard newCollectionItemFromBaseWindow = GetWindow<CreateNewCollectionItemFromBaseWizard>("Creating new derived item");
            newCollectionItemFromBaseWindow.minSize = new Vector2(350, 120);
            newCollectionItemFromBaseWindow.maxSize = new Vector2(350, 120);
            newCollectionItemFromBaseWindow.ShowPopup();
        }
        
        private void OnDisable()
        {
            onCreationCallback?.Invoke(false);
        }

        private void OnGUI()
        {
            if (targetType == null)
            {
                onCreationCallback?.Invoke(false);
                Close();
                return;
            }
            
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    EditorGUILayout.LabelField("Settings", EditorStyles.foldoutHeader);
                    EditorGUILayout.Space();

                    GUI.SetNextControlName("NameInputField");
                    newClassName = EditorGUILayout.TextField("Type Name", newClassName);
                    if (this.targetFolder == null)
                    {
                        string path = String.Empty;
                        if (!targetType.IsAbstract)
                        {
                            ScriptableObject instance = CreateInstance(targetType);
                            MonoScript scriptObj = MonoScript.FromScriptableObject(instance);
                            path = AssetDatabase.GetAssetPath(scriptObj);
                        }
                        else
                        {
                            if (CollectionsRegistry.Instance.TryGetCollectionFromItemType(targetType,
                                out ScriptableObjectCollection collection))
                            {
                                MonoScript scriptObj = MonoScript.FromScriptableObject(collection);
                                path = AssetDatabase.GetAssetPath(scriptObj);
                            }
                        }

                        if (string.IsNullOrEmpty(path))
                            path = "Assets/";
                        
                        this.targetFolder =
                            AssetDatabase.LoadAssetAtPath<DefaultAsset>(
                                Path.GetDirectoryName(path));
                    }

                    targetFolder = (DefaultAsset) EditorGUILayout.ObjectField("Script Folder",
                        targetFolder, typeof(DefaultAsset), false);

                    if (GUI.GetNameOfFocusedControl() != "NameInputField")
                    {
                        if (!string.IsNullOrEmpty(newClassName))
                            newClassName = newClassName.Sanitize();
                    }

                    GUILayout.Space(20);

                    using (new EditorGUI.DisabledScope(!AreSettingsValid()))
                    {
                        Color color = GUI.color;
                        GUI.color = Color.green;


                        if (GUILayout.Button("Create"))
                        {
                            string targetNamespace = String.Empty;

                            if (!string.IsNullOrEmpty(targetType.Namespace))
                                targetNamespace = targetType.Namespace;

                            targetNamespace = targetNamespace.TrimEnd('.');
                            
                            string parentFolder = AssetDatabase.GetAssetPath(targetFolder);
                            CodeGenerationUtility.CreateNewScript(newClassName,
                                parentFolder, targetNamespace, string.Empty,$"public class {newClassName} : {targetType}", null);
                            
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                            
                            if (string.IsNullOrEmpty(targetNamespace))
                                LastCollectionFullName.Value = $"{newClassName}";
                            else
                                LastCollectionFullName.Value = $"{targetNamespace}.{newClassName}";
                            
                            LastGeneratedCollectionScriptPath.Value = Path.Combine(parentFolder, $"{newClassName}.cs");
                            Close();
                            onCreationCallback.Invoke(true);
                        }

                        GUI.color = color;
                    }

                }
            }
        }

        private bool AreSettingsValid()
        {
            if (string.IsNullOrEmpty(newClassName))
                return false;

            if (targetFolder == null)
                return false;

            string parentFolder = AssetDatabase.GetAssetPath(targetFolder);
            if (AssetDatabase.LoadAssetAtPath<MonoScript>(Path.Combine(parentFolder, $"{newClassName}.cs")) != null)
                return false;

            return true;
        }
    }
}
