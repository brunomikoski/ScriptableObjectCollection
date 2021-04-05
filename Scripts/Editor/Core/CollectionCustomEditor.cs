using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomEditor(typeof(ScriptableObjectCollection), true)]
    public class CollectionCustomEditor : Editor
    {
        private const string WAITING_FOR_SCRIPT_TO_BE_CREATED_KEY = "WaitingForScriptTobeCreated";
        private static ScriptableObjectCollectionItem LAST_ADDED_COLLECTION_ITEM;

        private ScriptableObjectCollection collection;
        private string searchString = "";
        
        private List<ScriptableObjectCollectionItem> filteredItemList = new List<ScriptableObjectCollectionItem>();
        private readonly List<SerializedObject> filteredSerializedList = new List<SerializedObject>();
        private bool filteredItemListDirty = true;
        private SearchField searchField;
        private bool showSettings;

        private static bool IsWaitingForNewTypeBeCreated
        {
            get => EditorPrefs.GetBool(WAITING_FOR_SCRIPT_TO_BE_CREATED_KEY, false);
            set => EditorPrefs.SetBool(WAITING_FOR_SCRIPT_TO_BE_CREATED_KEY, value);
        }

        public void OnEnable()
        {
            collection = (ScriptableObjectCollection)target;

            if (!CollectionsRegistry.Instance.IsKnowCollection(collection))
                CollectionsRegistry.Instance.ReloadCollections();

            FixCollectionItems();
            ValidateGUIDS();
            CheckGeneratedCodeLocation();
            CheckIfCanBePartial();
            CheckGeneratedStaticFileName();
            CheckGeneratedStaticFileNamespace();
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

            for (int i = 0; i < collection.Count; i++)
            {
                collection[i].SetCollection(collection);
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
       }

        private void UpdateFilteredItemList()
        {
            if (!filteredItemListDirty)
                return;

            CheckForNullItemsOrType();
            
            filteredItemList.Clear();
            filteredSerializedList.Clear();

            IEnumerable<ScriptableObjectCollectionItem> items = collection.Items;
            if (string.IsNullOrEmpty(searchString))
            {
                filteredItemList = new List<ScriptableObjectCollectionItem>(items);
            }
            else
            {
                filteredItemList = items.Where(o => o.name.IndexOf(searchString, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    .ToList();
            }

            for (int i = 0; i < filteredItemList.Count; i++)
            {
                if (filteredItemList[i] == null)
                    continue;
                
                filteredSerializedList.Add(new SerializedObject(filteredItemList[i]));
            }

            filteredItemListDirty = false;
        }

        private void CheckForNullItemsOrType()
        {
            bool needToRewrite = false;
            
            SerializedProperty items = serializedObject.FindProperty("items");

            List<ScriptableObjectCollectionItem> validItems = new List<ScriptableObjectCollectionItem>();
            for (int i = 0; i < items.arraySize; i++)
            {
                SerializedProperty item = items.GetArrayElementAtIndex(i);
                if (item.propertyType != SerializedPropertyType.ObjectReference) 
                    continue;

                if (item.objectReferenceValue == null || item.objectReferenceInstanceIDValue == 0)
                    continue;
                    
                validItems.Add(item.objectReferenceValue as ScriptableObjectCollectionItem);
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
            List<Type> itemsSubclasses = TypeUtility.GetAllSubclasses(collection.GetItemType(), true);

            itemsSubclasses.Add(collection.GetItemType());
            GenericMenu optionsMenu = new GenericMenu();

            for (int i = 0; i < itemsSubclasses.Count; i++)
            {
                Type itemSubClass = itemsSubclasses[i];
                if (itemSubClass.IsAbstract)
                    continue;
                
                AddMenuOption(optionsMenu, itemSubClass.Name, () =>
                {
                    EditorApplication.delayCall += () => { AddNewItemOfType(itemSubClass); };
                });
            }
                
            optionsMenu.AddSeparator("");
            
            for (int i = 0; i < itemsSubclasses.Count; i++)
            {
                Type itemSubClass = itemsSubclasses[i];

                if (itemSubClass.IsSealed)
                    continue;
                
                AddMenuOption(optionsMenu, $"Create New/class $NEW : {itemSubClass.Name}", () =>
                {
                    EditorApplication.delayCall += () => { CreateAndAddNewItemOfType(itemSubClass); };
                });
            }
                
            optionsMenu.ShowAsContext();
        }

        private void CreateAndAddNewItemOfType(Type itemSubClass)
        {
            CreateNewCollectionItemFromBaseWizzard.Show(itemSubClass, success =>
            {
                if (success)
                {
                    IsWaitingForNewTypeBeCreated = true;
                }
            });
        }

        [DidReloadScripts]
        public static void AfterStaticAssemblyReload()
        {
            if (!IsWaitingForNewTypeBeCreated)
                return;

            IsWaitingForNewTypeBeCreated = false;

            string lastGeneratedCollectionScriptPath = CreateNewCollectionItemFromBaseWizzard.LastGeneratedCollectionScriptPath;
            string lastCollectionFullName = CreateNewCollectionItemFromBaseWizzard.LastCollectionFullName;

            if (string.IsNullOrEmpty(lastGeneratedCollectionScriptPath))
                return;
            
            CreateNewCollectionItemFromBaseWizzard.LastCollectionFullName = string.Empty;
            CreateNewCollectionItemFromBaseWizzard.LastGeneratedCollectionScriptPath = string.Empty;

            string assemblyName = CompilationPipeline.GetAssemblyNameFromScriptPath(lastGeneratedCollectionScriptPath);

            Type targetType = Type.GetType($"{lastCollectionFullName}, {assemblyName}");

            if (CollectionsRegistry.Instance.TryGetCollectionFromItemType(targetType,
                out ScriptableObjectCollection collection))
            {
                Selection.activeObject = null;
                LAST_ADDED_COLLECTION_ITEM =  collection.AddNew(targetType);
                EditorApplication.delayCall += () =>
                {
                    Selection.activeObject = collection;
                };
            }
        }
 
        private void AddNewItemOfType(Type targetType)
        {
            LAST_ADDED_COLLECTION_ITEM = collection.AddNew(targetType);
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
            ScriptableObjectCollectionItem collectionItem = filteredItemList[index];

            if (collectionItem == null)
            {
                filteredItemListDirty = true;
                return;
            }
            
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    CollectionUtility.SetFoldoutOpen(EditorGUILayout.Toggle(GUIContent.none,
                        CollectionUtility.IsFoldoutOpen(collectionItem, target), EditorStyles.foldout,
                        GUILayout.Width(13)), collectionItem, target);

                    using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
                    {
                        GUI.SetNextControlName(collectionItem.GUID);

                        string newName = EditorGUILayout.DelayedTextField(
                            collectionItem.name,
                            CollectionEditorGUI.ItemNameStyle, GUILayout.ExpandWidth(true)
                        );

                        if (LAST_ADDED_COLLECTION_ITEM == collectionItem)
                        {
                            EditorGUI.FocusTextInControl(collectionItem.GUID);
                            LAST_ADDED_COLLECTION_ITEM = null;
                        }
                        
                        if (changeCheck.changed)
                        {
                            if (newName.IsReservedKeyword())
                            {
                                Debug.LogError($"{newName} is a reserved C# keyword, will cause issues with " +
                                               $"code generation, reverting to previous name");
                            }
                            else
                            {
                                AssetDatabaseUtils.RenameAsset(collectionItem, newName);
                            }
                        }
                    }

                    DrawSelectItem(collectionItem);
                    DrawMoveItemDownButton(collectionItem);
                    DrawMoveItemUpButton(collectionItem);
                    DrawDeleteButton(collectionItem);
                }
                
                if (CollectionUtility.IsFoldoutOpen(collectionItem, target))
                {
                    EditorGUI.indentLevel++;
                    Editor editor = EditorsCache.GetOrCreateEditorForItem(collectionItem);
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

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Copy", EditorStyles.toolbarButton))
                        {
                            CopyCollectionItemUtils.SetSource(collectionItem);
                        }

                        using (new EditorGUI.DisabledScope(!CopyCollectionItemUtils.CanPasteToTarget(collectionItem)))
                        {
                            if (GUILayout.Button("Paste", EditorStyles.toolbarButton))
                            {
                                CopyCollectionItemUtils.ApplySourceToStart(collectionItem);
                            }
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawSelectItem(ScriptableObjectCollectionItem collectionItem)
        {
            if (GUILayout.Button(CollectionEditorGUI.ARROW_RIGHT_CHAR, EditorStyles.miniButton, GUILayout.Width(30),
                CollectionEditorGUI.DEFAULT_HEIGHT))
            {
                Selection.activeObject = collectionItem;
            }
        }

        private void DrawDeleteButton(ScriptableObjectCollectionItem item)
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
                filteredItemListDirty = true;
            }

            GUI.backgroundColor = previousColor;
        }

        private void DrawMoveItemDownButton(ScriptableObjectCollectionItem collectionItem)
        {
            int index = collection.IndexOf(collectionItem);
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

        private void DrawMoveItemUpButton(ScriptableObjectCollectionItem collectionItem)
        {
            int index = collection.IndexOf(collectionItem);
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

                    DrawAutomaticallyLoaded();
                    DrawGeneratedClassParentFolder();
                    DrawPartialClassToggle();
                    DrawGeneratedFileName();
                    DrawGeneratedFileNamespace();
                    
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawGeneratedFileName()
        {
            SerializedProperty fileNameSerializedProperty = serializedObject.FindProperty("generatedStaticClassFileName");
            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                string newFileName = EditorGUILayout.DelayedTextField("File Name", fileNameSerializedProperty.stringValue);
                if (changeCheck.changed)
                {
                    fileNameSerializedProperty.stringValue = newFileName;
                    fileNameSerializedProperty.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void DrawGeneratedFileNamespace()
        {
            SerializedProperty fileNamespaceSerializedProperty = serializedObject.FindProperty("generateStaticFileNamespace");
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
        
        private void DrawAutomaticallyLoaded()
        {
            SerializedProperty automaticLoadedSerializedProperty = serializedObject.FindProperty("automaticallyLoaded");
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
            SerializedProperty generatedCodePathSerializedProperty = serializedObject.FindProperty("generatedFileLocationPath");
            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                DefaultAsset pathObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>(generatedCodePathSerializedProperty.stringValue);
                pathObject = (DefaultAsset) EditorGUILayout.ObjectField(
                    "Generated Scripts Parent Folder",
                    pathObject,
                    typeof(DefaultAsset),
                    false
                );
                if (changeCheck.changed)
                {
                    generatedCodePathSerializedProperty.stringValue = AssetDatabase.GetAssetPath(pathObject);
                    generatedCodePathSerializedProperty.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void DrawPartialClassToggle()
        {
            SerializedProperty usePartialClassSerializedProperty = serializedObject.FindProperty("generateAsPartialClass");
            bool canBePartial= CheckIfCanBePartial();
            
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
        
        private void CheckGeneratedStaticFileName()
        {
            SerializedProperty fileNameSerializedProperty = serializedObject.FindProperty("generatedStaticClassFileName");
            if (!string.IsNullOrEmpty(fileNameSerializedProperty.stringValue))
                return;

            if (collection.name.Equals(collection.GetItemType().Name, StringComparison.Ordinal) 
                && serializedObject.FindProperty("generateAsPartialClass").boolValue)
            {
                fileNameSerializedProperty.stringValue = $"{collection.GetItemType().Name}Static";
            }
            else
            {
                fileNameSerializedProperty.stringValue = $"{collection.name}Static".Sanitize().FirstToUpper();
            }
            fileNameSerializedProperty.serializedObject.ApplyModifiedProperties();
        }
        
        private void CheckGeneratedStaticFileNamespace()
        {
            SerializedProperty fileNamespaceSerializedProperty = serializedObject.FindProperty("generateStaticFileNamespace");
            if (!string.IsNullOrEmpty(fileNamespaceSerializedProperty.stringValue))
                return;
            
            
            ScriptableObjectCollectionSettings settingsInstance = ScriptableObjectCollectionSettings.GetInstance();
            if (!string.IsNullOrEmpty(settingsInstance.DefaultNamespace))
            {
                fileNamespaceSerializedProperty.stringValue = settingsInstance.DefaultNamespace;
                fileNamespaceSerializedProperty.serializedObject.ApplyModifiedProperties();
                return;
            }


            fileNamespaceSerializedProperty.stringValue = collection.GetItemType().Namespace;
            fileNamespaceSerializedProperty.serializedObject.ApplyModifiedProperties();
        }

        private bool CheckIfCanBePartial()
        {
            SerializedProperty generatedCodePathSerializedProperty = serializedObject.FindProperty("generatedFileLocationPath");
            SerializedProperty usePartialClassSerializedProperty = serializedObject.FindProperty("generateAsPartialClass");
            string baseClassPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(collection));
            string baseAssembly = CompilationPipeline.GetAssemblyNameFromScriptPath(baseClassPath);
            string targetGeneratedCodePath = CompilationPipeline.GetAssemblyNameFromScriptPath(generatedCodePathSerializedProperty.stringValue);
            bool canBePartial = baseAssembly.Equals(targetGeneratedCodePath, StringComparison.Ordinal);
            if (usePartialClassSerializedProperty.boolValue && !canBePartial)
            {
                usePartialClassSerializedProperty.boolValue = false;
                usePartialClassSerializedProperty.serializedObject.ApplyModifiedProperties();
            }

            return canBePartial;
        }

        private void CheckGeneratedCodeLocation()
        {
            SerializedProperty generatedCodePathSerializedProperty = serializedObject.FindProperty("generatedFileLocationPath");
            if (!string.IsNullOrEmpty(generatedCodePathSerializedProperty.stringValue))
                return;

            ScriptableObjectCollectionSettings settingsInstance = ScriptableObjectCollectionSettings.GetInstance();
            if (!string.IsNullOrEmpty(settingsInstance.DefaultGeneratedScriptsPath))
            {
                generatedCodePathSerializedProperty.stringValue = settingsInstance.DefaultGeneratedScriptsPath;
                generatedCodePathSerializedProperty.serializedObject.ApplyModifiedProperties();
            }
            else
            {
                string collectionScriptPath =
                    Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(collection)));
                
                generatedCodePathSerializedProperty.stringValue = collectionScriptPath;
                generatedCodePathSerializedProperty.serializedObject.ApplyModifiedProperties();
            }
        }

        public static void SetLastAddedEnum(ScriptableObjectCollectionItem collectionItem)
        {
            LAST_ADDED_COLLECTION_ITEM = collectionItem;
        }
    }
}
