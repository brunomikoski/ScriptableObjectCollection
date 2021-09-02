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

        protected CollectionItemEditorOptionsAttribute OptionsAttribute
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
        private float totalHeight;

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

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return Mathf.Max(totalHeight, EditorGUIUtility.singleLineHeight);
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

            DrawCollectionItemDrawer(ref position, item, label,
                newItem =>
                {
                    property.objectReferenceValue = newItem;
                    property.serializedObject.ApplyModifiedProperties();
                });
        }

        public virtual void DrawCollectionItemDrawer(ref Rect position, ScriptableObjectCollectionItem collectionItem, GUIContent label, 
            Action<ScriptableObjectCollectionItem> callback)
        {
            float originY = position.y;
            position.height = 15;
            Rect prefixPosition = EditorGUI.PrefixLabel(position, label);
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            if (collectionItem != null)
            {
                if (currentObject == null)
                    currentObject = collectionItem;
                
                DrawEditFoldoutButton(ref prefixPosition, collectionItem);
                DrawGotoButton(ref prefixPosition, collectionItem);
            }

            DrawCollectionItemDropDown(ref prefixPosition, collectionItem, callback);
            DrawEditorPreview(ref position, collectionItem);
            EditorGUI.indentLevel = indent;
            totalHeight = position.y - originY;
        }

        protected virtual void DrawEditorPreview(ref Rect rect, ScriptableObjectCollectionItem scriptableObjectCollectionItem)
        {
            if (scriptableObjectCollectionItem == null)
                return;
            
            if (!CollectionUtility.IsFoldoutOpen(scriptableObjectCollectionItem, currentObject))
                return;

            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            rect.width -= 10;
            rect.y += 10;
            float beginPositionY = rect.y;

            SerializedObject collectionItemSerializedObject = new SerializedObject(scriptableObjectCollectionItem);

            EditorGUI.indentLevel++;
            rect = EditorGUI.IndentedRect(rect); 
            SerializedProperty iterator = collectionItemSerializedObject.GetIterator();

            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
                {
                    bool guiEnabled = GUI.enabled;
                    if (iterator.displayName.Equals("Script"))
                        GUI.enabled = false;

                    EditorGUI.PropertyField(rect, iterator, true);

                    GUI.enabled = guiEnabled;

                    rect.y += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
                }

                if (changeCheck.changed)
                    iterator.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.indentLevel--;
            rect = EditorGUI.IndentedRect(rect);

            Rect boxPosition = rect;
            boxPosition.y = beginPositionY - 5;
            boxPosition.height = (rect.y - beginPositionY)+10;
            boxPosition.width += 10;
            boxPosition.x += 5;
            rect.y += 10;
            GUI.Box(boxPosition, GUIContent.none, EditorStyles.helpBox);
        }

        public virtual void Initialize(SerializedProperty property)
        {
            if (initialized)
                return;
            
            Type arrayOrListType = fieldInfo.FieldType.GetArrayOrListType();
            Type itemType = arrayOrListType != null ? arrayOrListType : fieldInfo.FieldType;

            var obj = property.serializedObject.targetObject;
            
            Initialize(itemType, obj);
        }

        public virtual void Initialize(Type collectionItemType, Object obj)
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

        protected virtual void DrawCollectionItemDropDown(ref Rect position, ScriptableObjectCollectionItem collectionItem,
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

        protected virtual void DrawGotoButton(ref Rect popupRect, ScriptableObjectCollectionItem collectionItem)
        {
            Rect buttonRect = popupRect;
            buttonRect.width = 30;
            buttonRect.height = 18;
            popupRect.width -= buttonRect.width;
            buttonRect.x += popupRect.width;
            if (GUI.Button(buttonRect, CollectionEditorGUI.ARROW_RIGHT_CHAR))
            {
                Selection.activeObject = collectionItem.Collection;
                CollectionUtility.SetFoldoutOpen(true, this.item, collectionItem.Collection);
            }
        }

        protected virtual void DrawEditFoldoutButton(ref Rect popupRect, ScriptableObjectCollectionItem targetItem)
        {
            Rect buttonRect = popupRect;
            buttonRect.width = 30;
            buttonRect.height = 18;
            popupRect.width -= buttonRect.width;
            buttonRect.x += popupRect.width;

            GUIContent guiContent = CollectionEditorGUI.EditGUIContent;
            if (CollectionUtility.IsFoldoutOpen(targetItem, currentObject))
                guiContent = CollectionEditorGUI.CloseGUIContent;

            if (GUI.Button(buttonRect, guiContent))
            {
                CollectionUtility.SetFoldoutOpen(!CollectionUtility.IsFoldoutOpen(targetItem, currentObject), targetItem, currentObject);
                ObjectUtility.SetDirty(targetItem);
            }
        }

    }
}
