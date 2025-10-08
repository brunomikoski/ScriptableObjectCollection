using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Browser
{
#if UNITY_6000_1_OR_NEWER
    public class BrowserTreeView : TreeView<int>
#else
    public class BrowserTreeView : TreeView
#endif
    {
        public delegate void ItemClickedDelegate(BrowserTreeViewItem item);

        public event ItemClickedDelegate ItemClicked;

        private readonly GUIContent showCollectionContent = new("Show Collection");
        private readonly GUIContent hideCollectionContent = new("Hide Collection");
        private readonly GUIContent showHiddenCollectionsContent = new("Show Hidden Collections");
        private readonly GUIContent hideHiddenCollectionsContent = new("Hide Hidden Collections");

#if UNITY_6000_1_OR_NEWER
        public BrowserTreeView(TreeViewState<int> state)
            : base(state)
        {
            Initialize();
        }

        public BrowserTreeView(TreeViewState<int> state, MultiColumnHeader multiColumnHeader)
            : base(state, multiColumnHeader)
        {
            Initialize();
        }
#else
        public BrowserTreeView(TreeViewState state)
            : base(state)
        {
            Initialize();
        }

        public BrowserTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader)
            : base(state, multiColumnHeader)
        {
            Initialize();
        }
#endif

        private void Initialize()
        {
            showAlternatingRowBackgrounds = true;
            Reload();
        }

#if UNITY_6000_1_OR_NEWER
        protected override TreeViewItem<int> BuildRoot()
        {
            TreeViewItem<int> root = new(0, -1);
            int id = 1;

            string[] scriptableObjectCollections = AssetDatabase.FindAssets("t:ScriptableObjectCollection");
            foreach (string guid in scriptableObjectCollections)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObjectCollection collection =
                    AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(assetPath);

                if (BrowserSettings.Instance.IsHiddenCollection(collection.GetType()) &&
                    !BrowserSettings.Instance.ShowHiddenCollections)
                {
                    continue;
                }

                BrowserTreeViewItem parentItem = new(id++, 0, collection);

                foreach (ScriptableObject item in collection)
                {
                    BrowserTreeViewItem childItem = new(id++, 1, item);
                    parentItem.AddChild(childItem);
                }

                root.AddChild(parentItem);
            }

            if (root.children == null)
            {
                root.children = new List<TreeViewItem<int>>();
            }

            return root;
        }
#else
        protected override TreeViewItem BuildRoot()
        {
            TreeViewItem root = new TreeViewItem(0, -1);
            int id = 1;

            string[] scriptableObjectCollections = AssetDatabase.FindAssets("t:ScriptableObjectCollection");
            foreach (string guid in scriptableObjectCollections)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObjectCollection collection =
                    AssetDatabase.LoadAssetAtPath<ScriptableObjectCollection>(assetPath);

                if (BrowserSettings.Instance.IsHiddenCollection(collection.GetType()) &&
                    !BrowserSettings.Instance.ShowHiddenCollections)
                {
                    continue;
                }

                BrowserTreeViewItem parentItem = new BrowserTreeViewItem(id++, 0, collection);

                foreach (ScriptableObject item in collection)
                {
                    BrowserTreeViewItem childItem = new BrowserTreeViewItem(id++, 1, item);
                    parentItem.AddChild(childItem);
                }

                root.AddChild(parentItem);
            }

            if (root.children == null)
            {
                root.children = new List<TreeViewItem>();
            }

            return root;
        }
#endif

#if UNITY_6000_1_OR_NEWER
        protected override bool CanMultiSelect(TreeViewItem<int> item)
#else
        protected override bool CanMultiSelect(TreeViewItem item)
#endif
        {
            return false;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count != 1)
                return;

#if UNITY_6000_1_OR_NEWER
            TreeViewItem<int> item = FindItem(selectedIds[0], rootItem);
#else
            TreeViewItem item = FindItem(selectedIds[0], rootItem);
#endif

            if (item is BrowserTreeViewItem treeViewItem)
            {
                ItemClicked?.Invoke(treeViewItem);
            }
        }

        protected override void SingleClickedItem(int id)
        {
#if UNITY_6000_1_OR_NEWER
            TreeViewItem<int> item = FindItem(id, rootItem);
#else
            TreeViewItem item = FindItem(id, rootItem);
#endif

            if (item is BrowserTreeViewItem treeViewItem)
            {
                ItemClicked?.Invoke(treeViewItem);
            }
        }

        protected override void DoubleClickedItem(int id)
        {
#if UNITY_6000_1_OR_NEWER
            TreeViewItem<int> item = FindItem(id, rootItem);
#else
            TreeViewItem item = FindItem(id, rootItem);
#endif

            if (item is BrowserTreeViewItem treeViewItem)
            {
                EditorGUIUtility.PingObject(treeViewItem.ScriptableObject);
            }
        }

        protected override void ContextClickedItem(int id)
        {
#if UNITY_6000_1_OR_NEWER
            TreeViewItem<int> item = FindItem(id, rootItem);
#else
            TreeViewItem item = FindItem(id, rootItem);
#endif

            if (item is not BrowserTreeViewItem treeViewItem)
                return;

            GenericMenu menu = new();

            menu.AddItem(new GUIContent("Show In Project Window"),
                false,
                () => EditorGUIUtility.PingObject(treeViewItem.ScriptableObject));

            menu.AddSeparator(string.Empty);

            if (treeViewItem.ScriptableObject is ScriptableObjectCollection collection)
            {
                AddCollectionMenuItem(collection, menu);
            }
            else if (treeViewItem.ScriptableObject is ScriptableObjectCollectionItem collectionItem)
            {
                AddItemMenuItem(collectionItem, menu);
            }

            menu.AddSeparator(string.Empty);

            bool showHiddenCollections = BrowserSettings.Instance.ShowHiddenCollections;
            menu.AddItem(showHiddenCollections ? hideHiddenCollectionsContent : showHiddenCollectionsContent,
                false,
                () =>
                {
                    BrowserSettings.Instance.ShowHiddenCollections = !showHiddenCollections;
                    Reload();
                });

            menu.ShowAsContext();
        }

        private void AddCollectionMenuItem(ScriptableObjectCollection collection, GenericMenu menu)
        {
            bool isHidden = BrowserSettings.Instance.IsHiddenCollection(collection.GetType());
            GUIContent content = isHidden ? showCollectionContent : hideCollectionContent;

            if (BrowserSettings.Instance.CanHide(collection))
            {
                menu.AddItem(content,
                    false,
                    () =>
                    {
                        BrowserSettings.Instance.ToggleCollection(collection);
                        Reload();
                    });
            }
            else
            {
                menu.AddDisabledItem(content, isHidden);
            }
        }

        private void AddItemMenuItem(ScriptableObjectCollectionItem collectionItem, GenericMenu menu)
        {
            bool isHidden = BrowserSettings.Instance.IsHiddenCollection(collectionItem.Collection.GetType());
            GUIContent content = isHidden ? showCollectionContent : hideCollectionContent;

            if (BrowserSettings.Instance.CanHide(collectionItem.Collection))
            {
                menu.AddItem(content,
                    false,
                    () =>
                    {
                        BrowserSettings.Instance.ToggleCollection(collectionItem.Collection);
                        Reload();
                    });
            }
            else
            {
                menu.AddDisabledItem(content, isHidden);
            }
        }
    }
}
