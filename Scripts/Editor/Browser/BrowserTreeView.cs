using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Browser
{
    public class BrowserTreeView : TreeView
    {
        public delegate void ItemClickedDelegate(BrowserTreeViewItem item);

        public event ItemClickedDelegate ItemClicked;

        private readonly GUIContent showCollectionContent = new("Show Collection");
        private readonly GUIContent hideCollectionContent = new("Hide Collection");
        private readonly GUIContent showHiddenCollectionsContent = new("Show Hidden Collections");
        private readonly GUIContent hideHiddenCollectionsContent = new("Hide Hidden Collections");

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

        private void Initialize()
        {
            showAlternatingRowBackgrounds = true;
            // showBorder = true;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            TreeViewItem root = new(0, -1);
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

            return root;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void SingleClickedItem(int id)
        {
            TreeViewItem item = FindItem(id, rootItem);

            if (item is BrowserTreeViewItem treeViewItem)
            {
                ItemClicked?.Invoke(treeViewItem);
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            TreeViewItem item = FindItem(id, rootItem);

            if (item is BrowserTreeViewItem treeViewItem)
            {
                EditorGUIUtility.PingObject(treeViewItem.ScriptableObject);
            }
        }

        protected override void ContextClickedItem(int id)
        {
            TreeViewItem item = FindItem(id, rootItem);

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
