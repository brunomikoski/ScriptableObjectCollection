using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class CollectionEditorGUI
    {
        public const string ARROW_RIGHT_CHAR = "\u25BA";

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
                    normal = {textColor = EditorStyles.label.normal.textColor},
                };
                return cachedItemNameStyle;
            }
        }
    }
}
