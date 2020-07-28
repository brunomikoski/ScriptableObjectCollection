using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class CollectionEditorGUI
    {
        public const string DEFAULT_NONE_ITEM_TEXT = "None";
        public const string ARROW_UP_CHAR = "\u25B2";
        public const string ARROW_DOWN_CHAR = "\u25bc";
        public const string ARROW_RIGHT_CHAR = "\u25BA";
        public const string X_CHAR = "\u00D7";

        private static GUIContent cachedEditGUIContent;
        public static GUIContent EditGUIContent
        {
            get
            {
                if (cachedEditGUIContent != null)
                    return cachedEditGUIContent;

                cachedEditGUIContent = EditorGUIUtility.IconContent("Audio Mixer");
                cachedEditGUIContent.tooltip = "Edit";
                return cachedEditGUIContent;
            }
        }

        private static GUIContent cachedCloseGUIContent;
        public static GUIContent CloseGUIContent
        {
            get
            {
                if (cachedCloseGUIContent != null)
                    return cachedCloseGUIContent;

                cachedCloseGUIContent = EditorGUIUtility.IconContent("AssemblyLock");
                cachedCloseGUIContent.tooltip = "Close";
                return cachedCloseGUIContent;
            }
        }
        
        public static readonly GUILayoutOption DEFAULT_HEIGHT = GUILayout.Height(16);
        public static readonly Color DELETE_BUTTON_COLOR = new Color(0.75f, 0.2f, 0.2f, 1f);

        private static GUIStyle cachedItemNameStyle;
        public static GUIStyle ItemNameStyle
        {
            get
            {
                if (cachedItemNameStyle != null)
                    return cachedItemNameStyle;
                
                cachedItemNameStyle = new GUIStyle
                {
                    fontStyle = FontStyle.Bold,
                    margin = new RectOffset(0, 0, 2, 2),
                    alignment = TextAnchor.MiddleLeft,
                    normal = {textColor = EditorStyles.label.normal.textColor}
                };
                return cachedItemNameStyle;
            }
        }
    }
}
