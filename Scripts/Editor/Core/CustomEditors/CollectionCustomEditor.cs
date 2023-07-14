using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomEditor(typeof(ScriptableObjectCollection), true)]
    public class CollectionCustomEditor : Editor
    {
        private const string WAITING_FOR_SCRIPT_TO_BE_CREATED_KEY = "WaitingForScriptTobeCreated";
        private static ScriptableObject LAST_ADDED_COLLECTION_ITEM;

        protected ScriptableObjectCollection collection;
        private string searchString = "";

        private SearchField searchField;
        private bool showSettings;

        private float[] heights;
        private bool[] itemHidden;
        private ReorderableList reorderableList;
        private SerializedProperty itemsSerializedProperty;
        protected int lastCheckedForValidItemsArraySize;
        private readonly Dictionary<int, Rect> itemIndexToRect = new();
        private float? reorderableListYPosition;

        protected virtual bool DisplayAddButton => true;
        protected virtual bool DisplayRemoveButton => true;
        protected virtual bool AllowCustomTypeCreation => true;
        
        private static bool IsWaitingForNewTypeBeCreated
        {
            get => EditorPrefs.GetBool(WAITING_FOR_SCRIPT_TO_BE_CREATED_KEY, false);
            set => EditorPrefs.SetBool(WAITING_FOR_SCRIPT_TO_BE_CREATED_KEY, value);
        }

        protected virtual void OnEnable()
        {
            collection = (ScriptableObjectCollection)target;

            if (!CollectionsRegistry.Instance.IsKnowCollection(collection))
                CollectionsRegistry.Instance.ReloadCollections();
            
            itemsSerializedProperty = serializedObject.FindProperty("items");

            ValidateCollectionItems();

            CreateReorderableList();

            CheckGeneratedCodeLocation();
            CheckGeneratedStaticFileName();
            ValidateGeneratedFileNamespace();
        }

        private void CreateReorderableList()
        {
            reorderableList = new ReorderableList(serializedObject, itemsSerializedProperty, true, true, DisplayAddButton, DisplayRemoveButton);
            reorderableList.drawElementCallback += DrawCollectionItemAtIndex;
            reorderableList.elementHeightCallback += GetCollectionItemHeight;
            reorderableList.onAddDropdownCallback += OnClickToAddNewItem;
            reorderableList.onRemoveCallback += OnClickToRemoveItem;
            reorderableList.onReorderCallback += OnListOrderChanged;
            reorderableList.drawHeaderCallback += OnDrawerHeader;
        }

        protected virtual void OnDisable()
        {
            if (reorderableList == null)
                return;

            reorderableList.drawElementCallback -= DrawCollectionItemAtIndex;
            reorderableList.elementHeightCallback -= GetCollectionItemHeight;
            reorderableList.onAddDropdownCallback -= OnClickToAddNewItem;
            reorderableList.onRemoveCallback -= OnClickToRemoveItem;
            reorderableList.onReorderCallback -= OnListOrderChanged;
            reorderableList.drawHeaderCallback -= OnDrawerHeader;
        }

        private void OnDrawerHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Items", EditorStyles.boldLabel);
        }

        private void OnListOrderChanged(ReorderableList list)
        {
            list.serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        private void OnClickToRemoveItem(ReorderableList list)
        {
            int selectedIndex = list.index;
            RemoveItemAtIndex(selectedIndex);
        }

        protected void RemoveItemAtIndex(int selectedIndex)
        {
            SerializedProperty selectedProperty = reorderableList.serializedProperty.GetArrayElementAtIndex(selectedIndex);
            Object asset = selectedProperty.objectReferenceValue;
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(asset));
            AssetDatabase.SaveAssets();
            reorderableList.serializedProperty.DeleteArrayElementAtIndex(selectedIndex);
            reorderableList.serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        private void OnClickToAddNewItem(Rect buttonRect, ReorderableList list)
        {
            AddNewItem();
        }

        private float GetCollectionItemHeight(int index)
        {
            if (itemHidden == null || itemHidden.Length == 0 || itemHidden[index] || index > itemHidden.Length - 1)
                return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            return Mathf.Max(
                heights[index],
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing
            );
        }

        private void DrawCollectionItemAtIndex(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty collectionItemSerializedProperty = reorderableList.serializedProperty.GetArrayElementAtIndex(index);

            if (itemHidden[index] || collectionItemSerializedProperty.objectReferenceValue == null)
                return;
            
            float originY = rect.y;

            rect.height = EditorGUIUtility.singleLineHeight;
            rect.x += 10;
            rect.width -= 20;

            Rect foldoutArrowRect = rect;
            bool wasExpanded = collectionItemSerializedProperty.isExpanded;
            collectionItemSerializedProperty.isExpanded = EditorGUI.Foldout(
                foldoutArrowRect,
                collectionItemSerializedProperty.isExpanded,
                GUIContent.none
            );

            if (!wasExpanded && collectionItemSerializedProperty.isExpanded)
            {
                if (Event.current.alt)
                    SetAllExpanded(true);
            }
            else if (wasExpanded && !collectionItemSerializedProperty.isExpanded)
            {
                if (Event.current.alt)
                    SetAllExpanded(false);
            }

            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                string collectionItemName = "";
                if (!string.IsNullOrEmpty(collectionItemSerializedProperty.objectReferenceValue.name))
                    collectionItemName = collectionItemSerializedProperty.objectReferenceValue.name;

                if (!string.IsNullOrEmpty(collectionItemName))
                    GUI.SetNextControlName(collectionItemName);
                
                Rect nameRect = rect;
                string newName = EditorGUI.DelayedTextField(nameRect, collectionItemName, CollectionEditorGUI.ItemNameStyle);
                
                if (LAST_ADDED_COLLECTION_ITEM == collectionItemSerializedProperty.objectReferenceValue)
                {
                    EditorGUI.FocusTextInControl( collectionItemName);
                    reorderableList.index = index;
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
                        AssetDatabaseUtils.RenameAsset(collectionItemSerializedProperty.objectReferenceValue, newName);
                        AssetDatabase.SaveAssets();
                    }
                }
            }

            rect.y += EditorGUIUtility.singleLineHeight;

            if (collectionItemSerializedProperty.isExpanded)
            {
                rect.y += EditorGUIUtility.standardVerticalSpacing; 

                if (itemIndexToRect.TryGetValue(index, out Rect actualRect) && reorderableListYPosition.HasValue)
                {
                    actualRect.y = rect.y + reorderableListYPosition.Value + reorderableList.headerHeight;

                    // Have to indent the rect here because otherwise it overlaps with the drag handle
                    EditorGUI.indentLevel++;
                    actualRect = EditorGUI.IndentedRect(actualRect);
                    EditorGUI.indentLevel--;

                    GUILayout.BeginArea(actualRect);
                    EditorGUI.indentLevel++;

                    Editor editor = EditorCache.GetOrCreateEditorForObject(collectionItemSerializedProperty.objectReferenceValue);
                    editor.OnInspectorGUI();

                    EditorGUI.indentLevel--;
                    GUILayout.EndArea();

                    collectionItemSerializedProperty.serializedObject.ApplyModifiedProperties();
                    rect.y += actualRect.height;
                }
                else
                {
                    Rect verticalRect = EditorGUILayout.BeginVertical();
            
                    Editor editor = EditorCache.GetOrCreateEditorForObject(collectionItemSerializedProperty.objectReferenceValue);
                    editor.OnInspectorGUI();

                    EditorGUILayout.EndVertical();

                    if (Event.current.type == EventType.Repaint)
                    {
                        itemIndexToRect[index] = verticalRect;
                    }
                }
            }

            CheckForContextInputOnItem(collectionItemSerializedProperty, index, originY, rect);
    
            heights[index] = rect.y - originY;
        }

       

        private void SetAllExpanded(bool expanded)
        {
            for (int i = 0; i < reorderableList.count; i++)
            {
                SerializedProperty property = reorderableList.serializedProperty.GetArrayElementAtIndex(i);
                property.isExpanded = expanded;
            }
        }

        private void CheckForContextInputOnItem(SerializedProperty collectionItemSerializedProperty, int index, float originY, Rect rect)
        {
            Event current = Event.current;

            Rect contextRect = rect;
            contextRect.height = rect.y - originY;
            contextRect.y = originY;
            contextRect.x -= 30;
            contextRect.width += 50;
            
            if(contextRect.Contains(current.mousePosition) &&  current.type == EventType.ContextClick)
            {
                ScriptableObject scriptableObject = collectionItemSerializedProperty.objectReferenceValue as ScriptableObject;

                GenericMenu menu = new GenericMenu();

                menu.AddItem(
                    new GUIContent("Copy Values"),
                    false,
                    () =>
                    {
                        CopyCollectionItemUtility.SetSource(scriptableObject);
                    }
                );
                if (CopyCollectionItemUtility.CanPasteToTarget(scriptableObject))
                {
                    menu.AddItem(
                        new GUIContent("Paste Values"),
                        false,
                        () =>
                        {
                            CopyCollectionItemUtility.ApplySourceToTarget(scriptableObject);
                        }
                    );
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Paste Values"));
                }
                menu.AddSeparator("");

                menu.AddItem(
                    new GUIContent("Duplicate Item"),
                    false,
                    () =>
                    {
                        DuplicateItem(index);
                    }
                );
                
                menu.AddItem(
                    new GUIContent("Delete Item"),
                    false,
                    () =>
                    {
                        RemoveItemAtIndex(index);
                    }
                );
                
                menu.AddSeparator("");
                menu.AddItem(
                    new GUIContent("Select Asset"),
                    false,
                    () =>
                    {
                        SelectItemAtIndex(index);
                    }
                );
                
                menu.ShowAsContext();
 
                current.Use(); 
            }
        }

        private void SelectItemAtIndex(int index)
        {
            SerializedProperty serializedProperty = itemsSerializedProperty.GetArrayElementAtIndex(index);
            ScriptableObject collectionItem = serializedProperty.objectReferenceValue as ScriptableObject;
            Selection.objects = new Object[] { collectionItem };
        }

        private void DuplicateItem(int index)
        {
            SerializedProperty serializedProperty = itemsSerializedProperty.GetArrayElementAtIndex(index);
            ScriptableObject collectionItem = serializedProperty.objectReferenceValue as ScriptableObject;
            string collectionItemAssetPath = AssetDatabase.GetAssetPath(collectionItem);
            string path = Path.GetDirectoryName(collectionItemAssetPath);
            string cloneName = collectionItem.name + " Clone";
            if (AssetDatabase.CopyAsset(collectionItemAssetPath, $"{path}/{cloneName}.asset"))
            {
                AssetDatabase.SaveAssets();
                ScriptableObject clonedItem = AssetDatabase.LoadAssetAtPath<ScriptableObject>($"{path}/{cloneName}.asset");
                ISOCItem socItem = clonedItem as ISOCItem;
                if (socItem == null)
                    throw new Exception($"Cloned item {clonedItem.name} is not an ISOCItem");
                
                socItem.GenerateNewGUID();
                itemsSerializedProperty.InsertArrayElementAtIndex(index + 1);
                SerializedProperty clonedItemSerializedProperty = itemsSerializedProperty.GetArrayElementAtIndex(index + 1);
                clonedItemSerializedProperty.objectReferenceValue = clonedItem;
                clonedItemSerializedProperty.serializedObject.ApplyModifiedProperties();
                clonedItemSerializedProperty.isExpanded = true;
                LAST_ADDED_COLLECTION_ITEM = clonedItem;
            }
        }

        public override void OnInspectorGUI()
        {
            ValidateCollectionItems();
            CheckHeightsAndHiddenArraySizes();

            using (new GUILayout.VerticalScope("Box"))
            {
                DrawSearchField();
                DrawSynchronizeButton();
                reorderableList.DoLayoutList();
                if (Event.current.type == EventType.Repaint)
                {
                    reorderableListYPosition = GUILayoutUtility.GetLastRect().y;
                }
                DrawBottomMenu();
            }
            DrawSettings();
            CheckForKeyboardShortcuts();
        }

        private void CheckHeightsAndHiddenArraySizes()
        {
            if (heights == null || heights.Length != itemsSerializedProperty.arraySize)
                heights = new float[itemsSerializedProperty.arraySize];

            if (itemHidden == null || itemHidden.Length != itemsSerializedProperty.arraySize)
                itemHidden = new bool[itemsSerializedProperty.arraySize];
        }

        private void DrawSynchronizeButton()
        {
            if (GUILayout.Button("Synchronize Assets"))
            {
                collection.RefreshCollection();
                serializedObject.Update();
                CheckHeightsAndHiddenArraySizes();
            }
        }

        private void CheckForKeyboardShortcuts()
        {
            if (reorderableList.index == -1)
                return;

            if (!reorderableList.HasKeyboardControl())
                return;
            
            if (Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint)
                return;

            if (reorderableList.index > reorderableList.serializedProperty.arraySize - 1)
                return;
            
            SerializedProperty element = reorderableList.serializedProperty.GetArrayElementAtIndex(reorderableList.index);

            if (Event.current.keyCode == KeyCode.RightArrow)
            {
                element.isExpanded = true; 
                Event.current.Use();
            }
            else if (Event.current.keyCode == KeyCode.LeftArrow)
            {
                element.isExpanded = false; 
                Event.current.Use();
            }
        }

        private void ValidateCollectionItems()
        {
            if (lastCheckedForValidItemsArraySize == itemsSerializedProperty.arraySize)
                return;
            
            bool modified = false;
            for (int i = itemsSerializedProperty.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty itemProperty = itemsSerializedProperty.GetArrayElementAtIndex(i);
                if (itemProperty.objectReferenceValue == null)
                {
                    itemsSerializedProperty.DeleteArrayElementAtIndex(i);
                    modified = true;
                    Debug.LogWarning($"Removing SOCItem at index {i} because it is null");
                    continue;
                }

                ISOCItem socItem = (ScriptableObject) itemProperty.objectReferenceValue as ISOCItem;
                if (socItem == null)
                {
                    itemsSerializedProperty.DeleteArrayElementAtIndex(i);
                    modified = true;
                    Debug.LogWarning($"Removing SOCItem at index {i} because it is not a ISOCItem");
                    continue;
                }

                if (socItem.Collection == null)
                    socItem.SetCollection(collection);
            }

            if (modified)
                itemsSerializedProperty.serializedObject.ApplyModifiedProperties();
            
            lastCheckedForValidItemsArraySize = itemsSerializedProperty.arraySize;
        }

        private void DrawBottomMenu()
        {
            using (new EditorGUILayout.HorizontalScope("Box"))
            {
                if (GUILayout.Button($"Generate Static Access File", EditorStyles.miniButtonRight))
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
            List<Type> itemsSubclasses = new List<Type> {collection.GetItemType()};

            TypeCache.TypeCollection sub = TypeCache.GetTypesDerivedFrom(collection.GetItemType());
            for (int i = 0; i < sub.Count; i++)
            {
                itemsSubclasses.Add(sub[i]);
            }

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

            if (AllowCustomTypeCreation)
            {
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
            }

            optionsMenu.ShowAsContext();
        }

        private void CreateAndAddNewItemOfType(Type itemSubClass)
        {
            CreateNewCollectionItemFromBaseWizard.Show(itemSubClass, success =>
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

            string lastGeneratedCollectionScriptPath =
                CreateNewCollectionItemFromBaseWizard.LastGeneratedCollectionScriptPath.Value;
            string lastCollectionFullName = CreateNewCollectionItemFromBaseWizard.LastCollectionFullName.Value;

            if (string.IsNullOrEmpty(lastGeneratedCollectionScriptPath))
                return;
            
            CreateNewCollectionItemFromBaseWizard.LastCollectionFullName.Value = string.Empty;
            CreateNewCollectionItemFromBaseWizard.LastGeneratedCollectionScriptPath.Value = string.Empty;

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
            itemsSerializedProperty.arraySize++;
            SerializedProperty arrayElementAtIndex = itemsSerializedProperty.GetArrayElementAtIndex(itemsSerializedProperty.arraySize - 1);
            arrayElementAtIndex.objectReferenceValue = LAST_ADDED_COLLECTION_ITEM;
            arrayElementAtIndex.isExpanded = true;
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
                {
                    if (string.IsNullOrEmpty(searchString))
                    {
                        for (int i = 0; i < itemHidden.Length; i++)
                            itemHidden[i] = false;
                    }
                    else
                    {
                        for (int i = 0; i < itemsSerializedProperty.arraySize; i++)
                        {
                            SerializedProperty arrayElementAtIndex = itemsSerializedProperty.GetArrayElementAtIndex(i);
                            if (arrayElementAtIndex.objectReferenceValue.name.IndexOf(searchString, StringComparison.CurrentCultureIgnoreCase) == -1)
                                itemHidden[i] = true;
                        }
                    }
                }
            }

            EditorGUILayout.Separator();
        }

        private void DrawSettings()
        {
            using (new GUILayout.VerticalScope("Box"))
            {
                EditorGUI.indentLevel++;
                showSettings = EditorGUILayout.Foldout(showSettings, "Advanced", true);
                EditorGUI.indentLevel--;

                if (showSettings)
                {
                    EditorGUI.indentLevel++;

                    DrawAutomaticallyLoaded();
                    DrawGeneratedClassParentFolder();
                    DrawPartialClassToggle();
                    DrawUseBaseClassToggle();
                    DrawGeneratedFileName();
                    DrawGeneratedFileNamespace();
                    GUILayout.Space(10);
                    DrawDeleteCollection();
                    
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawDeleteCollection()
        {
            Color backgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Delete Collection"))
            {
                DeleteCollectionEditorWindow.DeleteCollection(collection);
            }

            GUI.backgroundColor = backgroundColor;
        }

        private void DrawGeneratedFileName()
        {
            using EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope();
            string newFileName = EditorGUILayout.DelayedTextField("Static File Name", serializedObject.FindProperty("generatedStaticClassFileName").stringValue);
            if (changeCheck.changed)
            {
                serializedObject.FindProperty("generatedStaticClassFileName").stringValue = newFileName;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawGeneratedFileNamespace()
        {
            using EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope();
            string newNameSpace = EditorGUILayout.DelayedTextField("Namespace", serializedObject.FindProperty("generateStaticFileNamespace").stringValue);
            if (changeCheck.changed)
            {
                serializedObject.FindProperty("generateStaticFileNamespace").stringValue = newNameSpace;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void ValidateGeneratedFileNamespace()
        {
            if (string.IsNullOrEmpty(serializedObject.FindProperty("generateStaticFileNamespace").stringValue))
            {
                if (collection != null)
                {
                    string targetNamespace = collection.GetItemType().Namespace;
                    if (!string.IsNullOrEmpty(targetNamespace))
                    {
                        serializedObject.FindProperty("generateStaticFileNamespace").stringValue = targetNamespace;
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }
        }
        
        private void DrawAutomaticallyLoaded()
        {
            using EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope();
            SerializedProperty autoLoadedSerializedProperty = serializedObject.FindProperty("automaticallyLoaded");
            bool isAutomaticallyLoaded = EditorGUILayout.Toggle("Automatically Loaded", autoLoadedSerializedProperty.boolValue);
            if (changeCheck.changed)
            {
                autoLoadedSerializedProperty.boolValue = isAutomaticallyLoaded;
                autoLoadedSerializedProperty.serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawGeneratedClassParentFolder()
        {
            using EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope();
            DefaultAsset pathObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>(serializedObject.FindProperty("generatedFileLocationPath").stringValue);
                
            if (pathObject == null && !string.IsNullOrEmpty(serializedObject.FindProperty("generatedFileLocationPath").stringValue))
            {
                pathObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>(SOCSettings.Instance.GeneratedScriptsDefaultFilePath);
                
            }
                
            pathObject = (DefaultAsset) EditorGUILayout.ObjectField(
                "Generated Scripts Parent Folder",
                pathObject,
                typeof(DefaultAsset),
                false
            );
            string assetPath = AssetDatabase.GetAssetPath(pathObject);

            if (changeCheck.changed || !string.Equals(serializedObject.FindProperty("generatedFileLocationPath").stringValue, assetPath, StringComparison.Ordinal))
            {
                serializedObject.FindProperty("generatedFileLocationPath").stringValue = assetPath;
                serializedObject.ApplyModifiedProperties();
                    

                if (string.IsNullOrEmpty(SOCSettings.Instance.GeneratedScriptsDefaultFilePath))
                {
                    SOCSettings.Instance.SetGeneratedScriptsDefaultFilePath(assetPath);
                }
            }
        }

        private void DrawPartialClassToggle()
        {
            bool canBePartial= CodeGenerationUtility.CheckIfCanBePartial(collection);
            
            EditorGUI.BeginDisabledGroup(!canBePartial);
            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                bool writeAsPartial = EditorGUILayout.Toggle("Write as Partial Class",
                    serializedObject.FindProperty("generateAsPartialClass").boolValue);
                
                if (changeCheck.changed)
                {
                    serializedObject.FindProperty("generateAsPartialClass").boolValue = writeAsPartial;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUI.EndDisabledGroup();
        }
        
        private void DrawUseBaseClassToggle()
        {
            using EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope();
            bool useBaseClass = EditorGUILayout.Toggle("Use Base Class for items", serializedObject.FindProperty("generateAsBaseClass").boolValue);
            if (changeCheck.changed)
            {
                serializedObject.FindProperty("generateAsBaseClass").boolValue = useBaseClass;
                serializedObject.ApplyModifiedProperties();
            }
        }
        
        private void CheckGeneratedStaticFileName()
        {
            if (!string.IsNullOrEmpty(serializedObject.FindProperty("generatedStaticClassFileName").stringValue))
                return;

            if (collection.name.Equals(collection.GetItemType().Name, StringComparison.Ordinal) 
                && serializedObject.FindProperty("generateAsPartialClass").boolValue)
            {
                serializedObject.FindProperty("generatedStaticClassFileName").stringValue =
                    $"{collection.GetItemType().Name}Static";
            }
            else
            {
                serializedObject.FindProperty("generatedStaticClassFileName").stringValue =
                    $"{collection.name}Static".Sanitize().FirstToUpper();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void CheckGeneratedCodeLocation()
        {
            if (!string.IsNullOrEmpty(serializedObject.FindProperty("generatedFileLocationPath").stringValue))
                return;

            if (!string.IsNullOrEmpty(SOCSettings.Instance.GeneratedScriptsDefaultFilePath))
            {
                serializedObject.FindProperty("generatedFileLocationPath").stringValue =
                    SOCSettings.Instance
                        .GeneratedScriptsDefaultFilePath;
            }
            else
            {
                string collectionScriptPath =
                    Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(collection)));

                serializedObject.FindProperty("generatedFileLocationPath").stringValue = collectionScriptPath;
            }
        }

        public static ScriptableObject AddNewItem(ScriptableObjectCollection collection, Type itemType)
        {
            ScriptableObject collectionItem = collection.AddNew(itemType);
            Selection.objects = new Object[] {collection};
            LAST_ADDED_COLLECTION_ITEM = collectionItem;
            return collectionItem;
        }
    }
}
