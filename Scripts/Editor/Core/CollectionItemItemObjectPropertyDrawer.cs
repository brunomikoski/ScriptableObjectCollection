using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomPropertyDrawer(typeof(ScriptableObjectCollectionItem), true)]
    public class CollectionItemItemObjectPropertyDrawer : PropertyDrawer
    {
        private static readonly CollectionItemEditorOptionsAttribute defaultAttribute
            = new CollectionItemEditorOptionsAttribute(DrawType.Dropdown);

        private CollectionItemEditorOptionsAttribute cachedOptionsAttribute;

        private CollectionItemEditorOptionsAttribute optionsAttribute
        {
            get
            {
                if (cachedOptionsAttribute == null)
                    cachedOptionsAttribute = GetOptionsAttribute();
                return cachedOptionsAttribute;
            }
        }

        private bool initialized;
        private ScriptableObjectCollection collection;
        
        private ScriptableObjectCollectionItem[] options;
        private string[] optionsNames;
        private GUIContent[] GUIContents;
        
        private ScriptableObjectCollectionItem item;
        private Object currentObject;

        private CollectionItemDropdown dropDown;

        ~CollectionItemItemObjectPropertyDrawer()
        {
            if(item.IsNull())
                return;

            ObjectUtility.SetDirty(item);
        }
        
        private CollectionItemEditorOptionsAttribute GetOptionsAttribute()
        {
            if (fieldInfo == null)
                return defaultAttribute;
            object[] attributes
                = fieldInfo.GetCustomAttributes(typeof(CollectionItemEditorOptionsAttribute), false);
            if (attributes.Length > 0)
                return attributes[0] as CollectionItemEditorOptionsAttribute;
            return defaultAttribute;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Initialize(property);
            
            item = property.objectReferenceValue as ScriptableObjectCollectionItem;

            if (optionsAttribute.DrawType == DrawType.AsReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;  
            }
            
            
            Rect popupRect = position;
            popupRect.height = 15;

            if (item != null)
            {
                DrawEditFoldoutButton(ref popupRect);
                DrawGotoButton(ref popupRect);
            }

            using (new EditorGUI.PropertyScope(position, label, property))
            {
                popupRect = EditorGUI.PrefixLabel(popupRect, label);

                int indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                bool showMixedValue = EditorGUI.showMixedValue;


                switch (optionsAttribute.DrawType)
                {
                    case DrawType.Dropdown:
                    {
                        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;

                        DrawSearchablePopup(popupRect, property);

                        break;
                    }

                    default:
                        throw new ArgumentOutOfRangeException("DrawType: " + optionsAttribute.DrawType);
                }

                if (item != null)
                {
                    if (CollectionUtility.IsFoldoutOpen(item, currentObject))
                    {
                        EditorGUI.indentLevel++;
                        using (new EditorGUILayout.VerticalScope("Box"))
                        {
                            Editor editor = EditorsCache.GetOrCreateEditorForItem(item);
                            using (EditorGUI.ChangeCheckScope changedCheck = new EditorGUI.ChangeCheckScope())
                            {
                                GUILayout.Space(10);
                                using (new EditorGUILayout.VerticalScope())
                                {
                                    editor.OnInspectorGUI();
                                }

                                EditorGUILayout.Space();

                                if (changedCheck.changed)
                                    property.serializedObject.ApplyModifiedProperties();
                            }
                        }
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUI.showMixedValue = showMixedValue;

                EditorGUI.indentLevel = indent;
            }
        }

        private void Initialize(SerializedProperty property)
        {
            if (initialized)
                return;
            
            Type itemType;
            Type arrayOrListType = fieldInfo.FieldType.GetArrayOrListType();
            if (arrayOrListType != null)
                itemType = arrayOrListType;
            else
                itemType = fieldInfo.FieldType;
            
            if (!CollectionsRegistry.Instance.TryGetCollectionFromItemType(itemType,
                out ScriptableObjectCollection resultCollection))
            {
                optionsAttribute.DrawType = DrawType.AsReference;
                return;
            }

            collection = resultCollection;

            options = collection.Items.ToArray();
            List<string> displayOptions = GetDisplayOptions();
            displayOptions.Insert(0, CollectionEditorGUI.DEFAULT_NONE_ITEM_TEXT);
            
            optionsNames = displayOptions.ToArray();
            GUIContents = optionsNames.Select(s => new GUIContent(s)).ToArray();

            currentObject = property.serializedObject.targetObject;
            initialized = true;
            
            dropDown = new CollectionItemDropdown(new AdvancedDropdownState(), collection);
        }

        private List<string> GetDisplayOptions()
        {
            return collection.Items.Select(o => o.name).ToList();
        }

        private void DrawSearchablePopup(Rect position, SerializedProperty property)
        {
            int selectedIndex = 0;

            if (item != null)
                selectedIndex = Array.IndexOf(options, item) + 1;
            
            if (GUI.Button(position, GUIContents[selectedIndex], EditorStyles.popup))
            {
                dropDown.Show(position, o =>
                {
                    property.objectReferenceValue = o;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
        }
        
        public void DrawGotoButton(ref Rect popupRect)
        {
            Rect buttonRect = popupRect;
            buttonRect.width = 30;
            buttonRect.height = 18;
            popupRect.width -= buttonRect.width;
            buttonRect.x += popupRect.width;
            if (GUI.Button(buttonRect, CollectionEditorGUI.ARROW_RIGHT_CHAR))
            {
                Selection.activeObject = collection;
                CollectionUtility.SetFoldoutOpen(true, item, collection);
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
