using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Browser
{
    public class BrowserEditorWindow : EditorWindow
    {
        [MenuItem("Window/Scriptable Object Collection Browser")]
        private static void Init()
        {
            BrowserEditorWindow wnd = GetWindow<BrowserEditorWindow>();
            wnd.titleContent = new GUIContent("Browser");
            wnd.Show();
        }

        private const float DIVIDER_MINIMUM = 0.2f;
        private const float DIVIDER_MAXIMUM = 0.8f;

        private BrowserTreeView treeView;
#if UNITY_6000_1_OR_NEWER
        private TreeViewState<int> treeViewState;
#else
        private TreeViewState treeViewState;
#endif
        private Editor itemEditor;
        private Vector2 scrollPosition;
        private int separatorPosition = 250;
        private bool isDragging;

        private bool viewSettings;

        private void OnEnable()
        {
#if UNITY_6000_1_OR_NEWER
            treeViewState ??= new TreeViewState<int>();
#else
            treeViewState ??= new TreeViewState();
#endif
            treeView = new BrowserTreeView(treeViewState);
            treeView.ItemClicked += OnItemClicked;

            BrowserSettings.Instance.SettingsChanged += OnSettingsChanged;
        }

        private void OnDisable()
        {
            BrowserSettings.Instance.SettingsChanged -= OnSettingsChanged;
        }

        private void OnItemClicked(BrowserTreeViewItem item)
        {
            itemEditor = Editor.CreateEditor(item.ScriptableObject);
        }

        private void OnSettingsChanged()
        {
            treeView?.Reload();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawTreeView();
            DrawItemEditor();
            DrawSeparator();
            HandleSeparatorMouse();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                treeView.Reload();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Settings", EditorStyles.toolbarButton))
            {
                SettingsService.OpenProjectSettings("Project/Scriptable Object Collection/Browser");
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTreeView()
        {
            Rect treeViewRect = new(0,
                EditorStyles.toolbar.fixedHeight,
                separatorPosition,
                position.height - EditorStyles.toolbar.fixedHeight);

            treeView.OnGUI(treeViewRect);
        }

        private void DrawItemEditor()
        {
            if (itemEditor == null)
                return;

            Rect editorRect = new(separatorPosition,
                EditorStyles.toolbar.fixedHeight,
                position.width - separatorPosition,
                position.height - EditorStyles.toolbar.fixedHeight);

            GUILayout.BeginArea(editorRect);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            itemEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawSeparator()
        {
            Rect rect = new(separatorPosition, EditorStyles.toolbar.fixedHeight, 1, position.yMax);
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? Color.black : Color.gray);
        }

        private void HandleSeparatorMouse()
        {
            Rect cursorRect = new(separatorPosition - 4, EditorStyles.toolbar.fixedHeight, 8, position.yMax);
            EditorGUIUtility.AddCursorRect(cursorRect, MouseCursor.ResizeHorizontal);

            Event evt = Event.current;
            if (!evt.isMouse)
                return;

            if (!cursorRect.Contains(evt.mousePosition) && !isDragging)
                return;

            if (evt.type == EventType.MouseDown)
            {
                isDragging = true;
            }

            if (evt.type == EventType.MouseUp)
            {
                isDragging = false;
            }

            if (evt.type == EventType.MouseDrag)
            {
                separatorPosition += Mathf.RoundToInt(evt.delta.x);
            }

            separatorPosition = Mathf.RoundToInt(Mathf.Clamp(separatorPosition,
                position.width * DIVIDER_MINIMUM,
                position.width * DIVIDER_MAXIMUM));

            evt.Use();
        }
    }
}
