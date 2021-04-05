using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomPropertyDrawer(typeof(ScriptableObjectCollectionItem), true)]
    public class CollectionItemItemPropertyDrawer : PropertyDrawer
    {
        private static readonly CollectionItemEditorOptionsAttribute DefaultAttribute
            = new CollectionItemEditorOptionsAttribute(DrawType.Dropdown);

        private CollectionItemEditorOptionsAttribute cachedOptionsAttribute;

        private CollectionItemEditorOptionsAttribute OptionsAttribute
        {
            get
            {
                if (cachedOptionsAttribute == null)
                    cachedOptionsAttribute = GetOptionsAttribute();
                return cachedOptionsAttribute;
            }
        }

        private bool initialized;
        
        private Object currentObject;

        private CollectionItemDropdown collectionItemDropdown;
        private ScriptableObjectCollectionItem item;

        private CollectionItemEditorOptionsAttribute GetOptionsAttribute()
        {
            if (fieldInfo == null)
                return DefaultAttribute;
            object[] attributes
                = fieldInfo.GetCustomAttributes(typeof(CollectionItemEditorOptionsAttribute), false);
            if (attributes.Length > 0)
                return attributes[0] as CollectionItemEditorOptionsAttribute;
            return DefaultAttribute;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Initialize(property);
            
            if (OptionsAttribute.DrawType == DrawType.AsReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;  
            }

            item = property.objectReferenceValue as ScriptableObjectCollectionItem;

            DrawCollectionItemDrawer(position, item, label,
                newItem =>
                {
                    property.objectReferenceValue = newItem;
                    property.serializedObject.ApplyModifiedProperties();
                });
        }

        internal void DrawCollectionItemDrawer(Rect position, ScriptableObjectCollectionItem collectionItem, GUIContent label, 
        Action<ScriptableObjectCollectionItem> callback)
        {
            position.height = 15;
            position = EditorGUI.PrefixLabel(position, label);
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            if (collectionItem != null)
            {
                DrawEditFoldoutButton(ref position);
                DrawGotoButton(ref position);
            }

            DrawCollectionItemDropDown(ref position, collectionItem, callback);
            DrawEditorPreview(collectionItem);
            EditorGUI.indentLevel = indent;
        }

        private void DrawEditorPreview(ScriptableObjectCollectionItem scriptableObjectCollectionItem)
        {
            if (scriptableObjectCollectionItem == null)
                return;
            
            if (!CollectionUtility.IsFoldoutOpen(scriptableObjectCollectionItem, currentObject))
                return;

            EditorGUI.indentLevel++;
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                Editor editor = EditorsCache.GetOrCreateEditorForItem(scriptableObjectCollectionItem);
                using (EditorGUI.ChangeCheckScope changedCheck = new EditorGUI.ChangeCheckScope())
                {
                    GUILayout.Space(10);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        editor.OnInspectorGUI();
                    }

                    EditorGUILayout.Space();
                    if (changedCheck.changed)
                    {
                        EditorUtility.SetDirty(scriptableObjectCollectionItem);
                    }
                }
            }

            EditorGUI.indentLevel--;
        }

        private void Initialize(SerializedProperty property)
        {
            if (initialized)
                return;
            
            Type arrayOrListType = fieldInfo.FieldType.GetArrayOrListType();
            Type itemType = arrayOrListType != null ? arrayOrListType : fieldInfo.FieldType;

            collectionItemDropdown = new CollectionItemDropdown(
                new AdvancedDropdownState(),
                itemType
            );
            
            currentObject = property.serializedObject.targetObject;
            initialized = true;
        }

        internal void Initialize(Type collectionItemType, Object obj)
        {
            if (initialized)
                return;
            
            collectionItemDropdown = new CollectionItemDropdown(
                new AdvancedDropdownState(),
                collectionItemType
            );
            
            currentObject = obj;
            initialized = true;
        }

        private void DrawCollectionItemDropDown(
            ref Rect position,
            ScriptableObjectCollectionItem collectionItem,
            Action<ScriptableObjectCollectionItem> callback)
        {
            GUIContent displayValue = new GUIContent("None");

            if (collectionItem != null)
                displayValue = new GUIContent(collectionItem.name);
            
            if (GUI.Button(position, displayValue, EditorStyles.popup))
            {
                collectionItemDropdown.Show(position, callback.Invoke);
            }
        }

        private void DrawGotoButton(ref Rect popupRect)
        {
            Rect buttonRect = popupRect;
            buttonRect.width = 30;
            buttonRect.height = 18;
            popupRect.width -= buttonRect.width;
            buttonRect.x += popupRect.width;
            if (GUI.Button(buttonRect, CollectionEditorGUI.ARROW_RIGHT_CHAR))
            {
                Selection.activeObject = item.Collection;
                CollectionUtility.SetFoldoutOpen(true, item, item.Collection);
            }
        }

        private void DrawEditFoldoutButton(ref Rect popupRect)
        {
            Rect buttonRect = popupRect;
            buttonRect.width = 30;
            buttonRect.height = 18;
            popupRect.width -= buttonRect.width;
            buttonRect.x += popupRect.width;

            GUIContent guiContent = CollectionEditorGUI.EditGUIContent;
            if (CollectionUtility.IsFoldoutOpen(item, currentObject))
                guiContent = CollectionEditorGUI.CloseGUIContent;

            if (GUI.Button(buttonRect, guiContent))
            {
                CollectionUtility.SetFoldoutOpen(!CollectionUtility.IsFoldoutOpen(item, currentObject), item, currentObject);
                ObjectUtility.SetDirty(item);
            }
        }

    }
}
