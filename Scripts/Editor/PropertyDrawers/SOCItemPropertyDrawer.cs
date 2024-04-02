using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
#if UNITY_2022_2_OR_NEWER
    [CustomPropertyDrawer(typeof(ISOCItem), true)]
#endif
    [CustomPropertyDrawer(typeof(ScriptableObjectCollectionItem), true)]
    public class SOCItemPropertyDrawer : PropertyDrawer
    {
        private const float BUTTON_WIDTH = 30;
        
        private static readonly SOCItemEditorOptionsAttribute DefaultAttribute = new();

        internal SOCItemEditorOptionsAttribute OptionsAttribute { get; private set; }

        private bool initialized;

        private Object currentObject;

        private CollectionItemDropdown collectionItemDropdown;
        private ScriptableObject item;
        private float totalHeight;
        
        private FieldInfo overrideFieldInfo;
        private FieldInfo TargetFieldInfo
        {
            get
            {
                if (overrideFieldInfo == null)
                    return fieldInfo;
                return overrideFieldInfo;
            }
        }
        
        private Type currentItemType;


        private SOCItemEditorOptionsAttribute GetOptionsAttribute()
        {
            if (TargetFieldInfo == null)
                return DefaultAttribute;
            object[] attributes = TargetFieldInfo.GetCustomAttributes(typeof(SOCItemEditorOptionsAttribute), false);
            if (attributes.Length > 0)
                return attributes[0] as SOCItemEditorOptionsAttribute;
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

            item = property.objectReferenceValue as ScriptableObject;

            EditorGUI.BeginProperty(position, label, property);
            DrawCollectionItemDrawer(ref position, property, item, label,
                newItem =>
                {
                    property.objectReferenceValue = newItem;
                    property.serializedObject.ApplyModifiedProperties();
                });
            EditorGUI.EndProperty();
        }

        internal void DrawCollectionItemDrawer(
            ref Rect position, SerializedProperty property, ScriptableObject collectionItem, GUIContent label,
            Action<ScriptableObject> callback)
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
            }
            
            DrawGotoButton(ref prefixPosition, collectionItem);
            DrawCollectionItemDropDown(ref prefixPosition, property, collectionItem, callback);
            DrawEditorPreview(ref position, collectionItem);
            EditorGUI.indentLevel = indent;
            totalHeight = position.y - originY;
        }

        private void DrawEditorPreview(ref Rect rect, ScriptableObject scriptableObject)
        {
            if (scriptableObject == null)
                return;

            if (!CollectionUtility.IsCollectionItemExpanded(scriptableObject, this))
                return;

            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            rect.width -= 10;
            rect.y += 10;
            float beginPositionY = rect.y;

            SerializedObject collectionItemSerializedObject = new(scriptableObject);

            EditorGUI.indentLevel++;
            rect = EditorGUI.IndentedRect(rect);
            SerializedProperty iterator = collectionItemSerializedObject.GetIterator();

            using (EditorGUI.ChangeCheckScope changeCheck = new())
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
            boxPosition.height = (rect.y - beginPositionY) + 10;
            boxPosition.width += 10;
            boxPosition.x += 5;
            rect.y += 10;
            GUI.Box(boxPosition, GUIContent.none, EditorStyles.helpBox);
        }

        private void Initialize(SerializedProperty property)
        {
            if (initialized)
                return;

            Type itemType;
            if (TargetFieldInfo == null)
            {
                Type parentType = property.serializedObject.targetObject.GetType();
                itemType = property.GetFieldInfoFromPathByType(parentType).FieldType;
            }
            else
            {
                Type arrayOrListType = TargetFieldInfo.FieldType.GetArrayOrListType();
                itemType = arrayOrListType ?? TargetFieldInfo.FieldType;
            }
            
            Initialize(itemType, property, GetOptionsAttribute());
        }

        internal void Initialize(Type collectionItemType, SOCItemEditorOptionsAttribute optionsAttribute)
        {
            Initialize(collectionItemType, null, optionsAttribute ?? GetOptionsAttribute());
        }

        internal void Initialize(
            Type collectionItemType, SerializedProperty serializedProperty, SOCItemEditorOptionsAttribute optionsAttribute)
        {
            if (initialized)
                return;

            
            OptionsAttribute = optionsAttribute;
            if (OptionsAttribute == null)
                OptionsAttribute = new SOCItemEditorOptionsAttribute();
            
            collectionItemDropdown = new CollectionItemDropdown(
                new AdvancedDropdownState(),
                collectionItemType,
                OptionsAttribute,
                serializedProperty
            );
            
            currentItemType = collectionItemType;
            currentObject = serializedProperty.serializedObject.targetObject;
            initialized = true;
            
        }

        private void DrawCollectionItemDropDown(
            ref Rect position, SerializedProperty property, ScriptableObject collectionItem,
            Action<ScriptableObject> callback)
        {
            GUIContent displayValue = new GUIContent("None");

            if (collectionItem != null)
                displayValue = new GUIContent(collectionItem.name);

            bool canUseDropDown = true;
            bool isDropdownError = false;

            // If the options are meant to be constrained to a specific collection, check if the collection specified
            // is valid. If not, draw some useful messages so you're aware what's wrong and know how to fix it.
            if (!string.IsNullOrEmpty(OptionsAttribute.ConstrainToCollectionField))
            {
                SerializedProperty collectionField = property.serializedObject.FindProperty(
                    OptionsAttribute.ConstrainToCollectionField);
                if (collectionField == null)
                {
                    displayValue.text = $"Invalid collection constraint '{OptionsAttribute.ConstrainToCollectionField}'";
                    canUseDropDown = false;
                    isDropdownError = true;
                }
                else
                {
                    ScriptableObjectCollection collectionToConstrainTo = collectionField
                        .objectReferenceValue as ScriptableObjectCollection;
                    if (collectionToConstrainTo == null)
                    {
                        displayValue.text = $"No collection specified.";
                        canUseDropDown = false;
                    }
                }
            }

            bool wasGuiEnabled = GUI.enabled;
            GUI.enabled = canUseDropDown;
            
            Color originalContentColor = GUI.contentColor;
            if (isDropdownError)
                GUI.contentColor = Color.red;
            
            if (GUI.Button(position, displayValue, EditorStyles.popup))
            {
                collectionItemDropdown.Show(position, callback.Invoke);
            }

            GUI.contentColor = originalContentColor;
            GUI.enabled = wasGuiEnabled;
        }

        private void DrawGotoButton(ref Rect popupRect, ScriptableObject collectionItem)
        {
            if (!OptionsAttribute.ShouldDrawGotoButton) 
                return;

            Rect buttonRect = popupRect;
            buttonRect.width = BUTTON_WIDTH;
            buttonRect.height = 18;
            popupRect.width -= buttonRect.width;
            buttonRect.x += popupRect.width;
            if (GUI.Button(buttonRect, CollectionEditorGUI.ARROW_RIGHT_CHAR))
            {

                if (collectionItem == null)
                {
                    if (CollectionsRegistry.Instance.TryGetCollectionsOfItemType(currentItemType, out List<ScriptableObjectCollection> possibleCollections))
                    {
                        Selection.activeObject = possibleCollections.First();
                    }
                }
                else
                {
                    if (collectionItem is not ISOCItem socItem)
                        return;
                        
                    Selection.activeObject = socItem.Collection;
                    CollectionUtility.SetOnlyCollectionItemExpanded(socItem, socItem.Collection);
                }
            }
        }

        private void DrawEditFoldoutButton(ref Rect popupRect, ScriptableObject targetItem)
        {
            if (!OptionsAttribute.ShouldDrawPreviewButton) 
                return;

            Rect buttonRect = popupRect;
            buttonRect.width = BUTTON_WIDTH;
            buttonRect.height = 18;
            popupRect.width -= buttonRect.width;
            buttonRect.x += popupRect.width;

            GUIContent guiContent = CollectionEditorGUI.EditGUIContent;

            if (CollectionUtility.IsCollectionItemExpanded(targetItem, this))
                guiContent = CollectionEditorGUI.CloseGUIContent;

            if (GUI.Button(buttonRect, guiContent))
            {
                bool isCollectionItemExpanded = CollectionUtility.IsCollectionItemExpanded(targetItem, this);
                CollectionUtility.SetCollectionItemExpanded(!isCollectionItemExpanded, targetItem, this);
            }
        }

        public void OverrideFieldInfo(FieldInfo targetFieldInfo)
        {
            overrideFieldInfo = targetFieldInfo;
        }
    }
}
