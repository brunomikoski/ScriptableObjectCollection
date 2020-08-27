using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomEditor(typeof(ScriptableObjectCollection), true)]
    public class ScriptableObjectCollectionCustomEditor : Editor
    {
        private ScriptableObjectCollection collection;
        private string searchString = "";
        
        private List<CollectableScriptableObject> filteredItemList = new List<CollectableScriptableObject>();
        private readonly List<SerializedObject> filteredSerializedList = new List<SerializedObject>();
        private bool filteredItemListDirty = true;
        private SearchField searchField;
        private bool showSettings;

        public void OnEnable()
        {
            collection = (ScriptableObjectCollection)target;

            if (!CollectionsRegistry.Instance.IsKnowCollectionGUID(collection.GUID))
                CollectionsRegistry.Instance.ReloadCollections();
            

            FixCollectionItems();
            ValidateGUIDS();
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
            DrawSettings(collection);
       }

        private void UpdateFilteredItemList()
        {
            if (!filteredItemListDirty)
                return;
            
            filteredItemList.Clear();
            filteredSerializedList.Clear();

            if (string.IsNullOrEmpty(searchString))
                filteredItemList = new List<CollectableScriptableObject>(collection.Items);
            else
            {
                filteredItemList = collection.Items.Where(o =>
                    o.name.IndexOf(searchString, StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();
            }

            for (int i = 0; i < filteredItemList.Count; i++)
            {
                filteredSerializedList.Add(new SerializedObject(filteredItemList[i]));
            }

            filteredItemListDirty = false;
        }

        private void DrawBottomMenu()
        {
            using (new EditorGUILayout.HorizontalScope("Box"))
            {
                if (GUILayout.Button("Add New", EditorStyles.miniButtonLeft))
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
            if (collectableSubclasses.Count == 0)
            {
                EditorApplication.delayCall += () => { AddNewItemOfType(collection.GetCollectionType()); };
            }
            else
            {
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
                optionsMenu.ShowAsContext();
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
                    Editor editor = CollectionUtility.GetEditorForItem(collectionItem);
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

        private void DrawSettings(ScriptableObjectCollection collection)
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
                    }

                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}
