using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomEditor(typeof(ScriptableObjectCollection), true)]
    public class ScriptableObjectCollectionCustomEditor : Editor
    {
        public const string WAITING_FOR_SCRIPT_TO_BE_CREATED_KEY = "WaitingForScriptTobeCreated";
        
        [Flags]
        private enum Warning
        {
            PartialStaticFileInDifferentAssembly = 1 << 0
        }
        
        private ScriptableObjectCollection collection;
        private string searchString = "";
        
        private List<CollectableScriptableObject> filteredItemList = new List<CollectableScriptableObject>();
        private readonly List<SerializedObject> filteredSerializedList = new List<SerializedObject>();
        private bool filteredItemListDirty = true;
        private SearchField searchField;
        private bool showSettings;
        private Warning warnings;


        private static bool isWaitingForNewTypeBeCreated
        {
            get => EditorPrefs.GetBool(WAITING_FOR_SCRIPT_TO_BE_CREATED_KEY, false);
            set => EditorPrefs.SetBool(WAITING_FOR_SCRIPT_TO_BE_CREATED_KEY, value);
        }

        public void OnEnable()
        {
            collection = (ScriptableObjectCollection)target;

            if (!CollectionsRegistry.Instance.IsKnowCollectionGUID(collection.GUID))
                CollectionsRegistry.Instance.ReloadCollections();
            

            FixCollectionItems();
            ValidateGUIDS();
            CheckForWarnings();
        }

        private void CheckForWarnings()
        {
            bool isGeneratingCustomStaticFile = ScriptableObjectCollectionSettings.Instance.IsGeneratingCustomStaticFile(collection);
            if (!isGeneratingCustomStaticFile)
            {
                MonoScript collectionMonoAsset = MonoScript.FromScriptableObject(collection);
                string collectionScriptPath = AssetDatabase.GetAssetPath(collectionMonoAsset);
                string collectionAssemblyPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(collectionScriptPath);
                if (!string.IsNullOrEmpty(collectionAssemblyPath))
                {
                    string generatedFilePath =  ScriptableObjectCollectionSettings.Instance.GetStaticFileFolderForCollection(collection);
                    string staticAssembly =
                        CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(generatedFilePath);

                    if (!string.Equals(collectionAssemblyPath, staticAssembly, StringComparison.Ordinal))
                    {
                        warnings |= Warning.PartialStaticFileInDifferentAssembly;
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (collection == null)
                return;

            for (int i = 0; i < collection.Items.Count; i++)
            {
                ObjectUtility.SetDirty(collection.Items[i]);
            }

            ObjectUtility.SetDirty(collection);
        }

        private void ValidateGUIDS()
        {
            collection.ValidateGUID();
        }

        private void FixCollectionItems()
        {
            for (int i = 0; i < collection.Count; i++)
            {
                if (collection[i] == null)
                {
                    collection.ClearBadItems();
                    return;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            using (new GUILayout.VerticalScope("Box"))
            {
                UpdateFilteredItemList();
                DrawSearchField();
                DrawItems();
                DrawBottomMenu();
            }
            DrawSettings();
            DrawWarnings();
       }

        private void DrawWarnings()
        {
            if (warnings.HasFlag(Warning.PartialStaticFileInDifferentAssembly))
            {
                EditorGUILayout.HelpBox(
                    "Static File Location is on a different assembly from the Collection script Assembly, " +
                    "please set to use a Custom Static File on the Collection Settings to generate a unique non-partial file",
                    MessageType.Error);
            }
        }

        private void UpdateFilteredItemList()
        {
            if (!filteredItemListDirty)
                return;

            CheckForNullItemsOrType();
            
            filteredItemList.Clear();
            filteredSerializedList.Clear();

            IEnumerable<CollectableScriptableObject> collectableScriptableObjects = collection.Items;
            if (string.IsNullOrEmpty(searchString))
            {
                filteredItemList = new List<CollectableScriptableObject>(collectableScriptableObjects);
            }
            else
            {
                filteredItemList = collectableScriptableObjects.Where(o => o.name.IndexOf(searchString, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    .ToList();
            }

            for (int i = 0; i < filteredItemList.Count; i++)
            {
                filteredSerializedList.Add(new SerializedObject(filteredItemList[i]));
            }

            filteredItemListDirty = false;
        }

        private void CheckForNullItemsOrType()
        {
            bool needToRewrite = false;
            
            SerializedProperty items = serializedObject.FindProperty("items");

            List<CollectableScriptableObject> validItems = new List<CollectableScriptableObject>();
            for (int i = 0; i < items.arraySize; i++)
            {
                SerializedProperty item = items.GetArrayElementAtIndex(i);
                if (item.propertyType != SerializedPropertyType.ObjectReference) 
                    continue;

                if (item.objectReferenceValue == null || item.objectReferenceInstanceIDValue == 0)
                {
                    Debug.LogError($"Removing item at position {i} since it has a null script",
                        serializedObject.context);
                    continue;
                }
                    
                validItems.Add(item.objectReferenceValue as CollectableScriptableObject);
                needToRewrite = true;
            }

            if (needToRewrite)
            {
                items.arraySize = validItems.Count;
                for (int i = 0; i < validItems.Count; i++)
                {
                    items.GetArrayElementAtIndex(i).objectReferenceValue = validItems[i];
                }
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawBottomMenu()
        {
            using (new EditorGUILayout.HorizontalScope("Box"))
            {
                if (GUILayout.Button("ADD", EditorStyles.miniButtonLeft))
                {
                    AddNewItem();
                }

                if (GUILayout.Button($"Generate Static File", EditorStyles.miniButtonRight))
                {
                    EditorApplication.delayCall += () =>
                    {
                        CodeGenerationUtility.GenerateStaticCollectionScript(collection);
                    };
                }
            }
        }
        
        private void AddNewItem()
        {
            List<Type> collectableSubclasses = TypeUtility.GetAllSubclasses(collection.GetCollectionType(), true);

            GenericMenu optionsMenu = new GenericMenu();
            if (!collection.GetCollectionType().IsAbstract)
            {
                AddMenuOption(optionsMenu,  collection.GetCollectionType().Name, () =>
                {
                    EditorApplication.delayCall += () => { AddNewItemOfType(collection.GetCollectionType()); };
                });
            }

            for (int i = 0; i < collectableSubclasses.Count; i++)
            {
                Type collectableSubClass = collectableSubclasses[i];
                AddMenuOption(optionsMenu, collectableSubClass.Name, () =>
                {
                    EditorApplication.delayCall += () => { AddNewItemOfType(collectableSubClass); };
                });
            }
                
            AddMenuOption(optionsMenu, $"Create new Type : {collection.GetCollectionType().Name}", () =>
            {
                EditorApplication.delayCall += () => { CreateAndAddNewItemOfType(collection.GetCollectionType()); };
            });
                
            optionsMenu.ShowAsContext();
        }

        private void CreateAndAddNewItemOfType(Type collectableSubClass)
        {
            CreateNewCollectableType.Show(collectableSubClass, success =>
            {
                if (success)
                {
                    isWaitingForNewTypeBeCreated = true;
                }
            });
        }

        [DidReloadScripts]
        public static void AfterStaticAssemblyReload()
        {
            if (!isWaitingForNewTypeBeCreated)
                return;

            isWaitingForNewTypeBeCreated = false;

            string lastGeneratedCollectionScriptPath = CreateNewCollectableType.LastGeneratedCollectionScriptPath;
            string lastCollectionFullName = CreateNewCollectableType.LastCollectionFullName;

            if (string.IsNullOrEmpty(lastGeneratedCollectionScriptPath))
                return;
            
            CreateNewCollectableType.LastCollectionFullName = string.Empty;
            CreateNewCollectableType.LastGeneratedCollectionScriptPath = string.Empty;

            string assemblyName = CompilationPipeline.GetAssemblyNameFromScriptPath(lastGeneratedCollectionScriptPath);

            Type targetType = Type.GetType($"{lastCollectionFullName}, {assemblyName}");

            if (CollectionsRegistry.Instance.TryGetCollectionForType(targetType,
                out ScriptableObjectCollection collection))
            {
                Selection.activeObject = null;
                collection.AddNew(targetType);
                EditorApplication.delayCall += () =>
                {
                    Selection.activeObject = collection;
                };
            }
        }
 
        private void AddNewItemOfType(Type targetType)
        {
            collection.AddNew(targetType);
            filteredItemListDirty = true;
        }

        private void AddMenuOption(GenericMenu optionsMenu, string displayName, Action action)
        {
            optionsMenu.AddItem(new GUIContent(displayName), false, action.Invoke);
        }

        private void DrawSearchField()
        {                
            Rect searchRect =
                GUILayoutUtility.GetRect(1, 1, 20, 20, GUILayout.ExpandWidth(true));

            if (searchField == null)
                searchField = new SearchField();

            using (EditorGUI.ChangeCheckScope changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                searchString = searchField.OnGUI(searchRect, searchString);
                
                if (changeCheckScope.changed)
                    filteredItemListDirty = true;
            }

            EditorGUILayout.Separator();
        }

        private void DrawItems()
        {
            using (new GUILayout.VerticalScope())
            {
                for (int i = 0; i < filteredItemList.Count; i++)
                {
                    DrawItem(i);
                }
            }
        }

        private void DrawItem(int index)
        {
            CollectableScriptableObject collectionItem = filteredItemList[index];

            if (collectionItem == null)
            {
                filteredItemListDirty = true;
                return;
            }
            
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    CollectionUtility.SetFoldoutOpen(collectionItem,
                        EditorGUILayout.Toggle(GUIContent.none, CollectionUtility.IsFoldoutOpen(collectionItem), EditorStyles.foldout,
                            GUILayout.Width(13)));

                    using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
                    {
                        string newName = EditorGUILayout.DelayedTextField(collectionItem.name, CollectionEditorGUI.ItemNameStyle,
                            GUILayout.ExpandWidth(true));

                        if (changeCheck.changed)
                            AssetDatabaseUtils.RenameAsset(collectionItem, newName);
                    }

                    DrawSelectItem(collectionItem);
                    DrawMoveItemDownButton(collectionItem);
                    DrawMoveItemUpButton(collectionItem);
                    DrawDeleteButton(collectionItem);
                }
                
                if (CollectionUtility.IsFoldoutOpen(collectionItem))
                {
                    EditorGUI.indentLevel++;
                    Editor editor = CollectionUtility.GetOrCreateEditorForItem(collectionItem);
                    using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
                    {
                        GUILayout.Space(10);
                        editor.OnInspectorGUI();
                        EditorGUILayout.Space();

                        if (changeCheck.changed)
                        {
                            if (index > filteredSerializedList.Count - 1 || filteredSerializedList[index] == null)
                                filteredItemListDirty = true;
                            else
                                filteredSerializedList[index].ApplyModifiedProperties();
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawSelectItem(CollectableScriptableObject collectionItem)
        {
            if (GUILayout.Button(CollectionEditorGUI.ARROW_RIGHT_CHAR, EditorStyles.miniButton, GUILayout.Width(30),
                CollectionEditorGUI.DEFAULT_HEIGHT))
            {
                Selection.activeObject = collectionItem;
            }
        }

        private void DrawDeleteButton(CollectableScriptableObject item)
        {
            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = CollectionEditorGUI.DELETE_BUTTON_COLOR;
            if (GUILayout.Button(CollectionEditorGUI.X_CHAR, EditorStyles.miniButton, GUILayout.Width(30),
                CollectionEditorGUI.DEFAULT_HEIGHT))
            {
                int index = collection.IndexOf(item);
                collection.RemoveAt(index);
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(item));
                ObjectUtility.SetDirty(collection);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                filteredItemListDirty = true;
            }

            GUI.backgroundColor = previousColor;
        }

        private void DrawMoveItemDownButton(CollectableScriptableObject collectable)
        {
            int index = collection.IndexOf(collectable);
            EditorGUI.BeginDisabledGroup(index >= collection.Count - 1);
            {
                if (GUILayout.Button(CollectionEditorGUI.ARROW_DOWN_CHAR, EditorStyles.miniButtonLeft, GUILayout.Width(30), CollectionEditorGUI.DEFAULT_HEIGHT))
                {
                    collection.Swap(index, index + 1);
                    filteredItemListDirty = true;
                    return;
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawMoveItemUpButton(CollectableScriptableObject collectable)
        {
            int index = collection.IndexOf(collectable);
            EditorGUI.BeginDisabledGroup(index <= 0);
            {
                if (GUILayout.Button(CollectionEditorGUI.ARROW_UP_CHAR, EditorStyles.miniButtonRight, GUILayout.Width(30), CollectionEditorGUI.DEFAULT_HEIGHT))
                {
                    collection.Swap(index, index - 1);
                    filteredItemListDirty = true;
                    return;
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawSettings()
        {
            using (new GUILayout.VerticalScope("Box"))
            {
                EditorGUI.indentLevel++;
                showSettings = EditorGUILayout.Foldout(showSettings, "Settings", true);
                EditorGUI.indentLevel--;

                if (showSettings)
                {
                    EditorGUI.indentLevel++;

                    using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
                    {
                        bool isAutomaticallyLoaded = EditorGUILayout.ToggleLeft("Automatically Loaded",
                            ScriptableObjectCollectionSettings.Instance.IsCollectionAutomaticallyLoaded(
                                collection));

                        if (changeCheck.changed)
                        {
                            ScriptableObjectCollectionSettings.Instance.SetCollectionAutomaticallyLoaded(
                                collection,
                                isAutomaticallyLoaded);
                        }
                    }

                    using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
                    {
                        GeneratedStaticFileType staticCodeGeneratorType =
                            (GeneratedStaticFileType) EditorGUILayout.EnumPopup("Static File Generator Type",
                                ScriptableObjectCollectionSettings.Instance.GetStaticFileTypeForCollection(
                                    collection));

                        if (changeCheck.changed)
                        {
                            ScriptableObjectCollectionSettings.Instance.SetStaticFileGeneratorTypeForCollection(
                                collection,
                                staticCodeGeneratorType);
                        }
                    }

                    bool overwriteStaticFileLocation = false;
                    using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
                    {
                        overwriteStaticFileLocation = EditorGUILayout.ToggleLeft(
                            "Overwrite Static File Location",
                            ScriptableObjectCollectionSettings.Instance.IsOverridingStaticFileLocation(collection));
                        if (changeCheck.changed)
                        {
                            ScriptableObjectCollectionSettings.Instance.SetOverridingStaticFileLocation(
                                collection, overwriteStaticFileLocation);
                        }
                    }

                    if (overwriteStaticFileLocation)
                    {
                        EditorGUI.indentLevel++;

                        DefaultAsset targetFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(
                            ScriptableObjectCollectionSettings.Instance.GetStaticFileFolderForCollection(
                                collection));
                        using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
                        {
                            targetFolder = (DefaultAsset) EditorGUILayout.ObjectField("Target Folder",
                                targetFolder,
                                typeof(DefaultAsset), false);

                            if (changeCheck.changed)
                            {
                                ScriptableObjectCollectionSettings.Instance.SetStaticFileFolderForCollection(
                                    collection,
                                    AssetDatabase.GetAssetPath(targetFolder));
                            }
                        }
                        EditorGUI.indentLevel--;
                    }

                    bool generateCustomStaticFile =
                        ScriptableObjectCollectionSettings.Instance.IsGeneratingCustomStaticFile(collection);
                    using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
                    {
                        generateCustomStaticFile = EditorGUILayout.ToggleLeft("Custom Static file", generateCustomStaticFile);
                        
                        if (changeCheck.changed)
                        {
                            ScriptableObjectCollectionSettings.Instance.SetGenerateCustomStaticFile(
                                collection, generateCustomStaticFile);
                        }
                    }

                    if (generateCustomStaticFile)
                    {
                        EditorGUI.indentLevel++;

                        string customStaticFileName =
                            ScriptableObjectCollectionSettings.Instance.GetGeneratedStaticFileName(collection);
                        using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
                        {
                            customStaticFileName = EditorGUILayout.TextField("Static Class Name", customStaticFileName);
                        
                            if (changeCheck.changed)
                            {
                                ScriptableObjectCollectionSettings.Instance.SetGenerateCustomStaticFileName(
                                    collection, customStaticFileName);
                            }
                        }
                        EditorGUI.indentLevel--;
                    }

                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}
