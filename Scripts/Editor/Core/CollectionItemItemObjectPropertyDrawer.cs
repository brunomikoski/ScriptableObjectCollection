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
        private ScriptableObjectCollection collection;
        
        private ScriptableObjectCollectionItem[] options;
        private GUIContent[] guiContents;
        
        private ScriptableObjectCollectionItem item;
        private Object currentObject;

        private CollectionItemDropdown collectionItemDropdown;

        private ScriptableObjectCollection[] availableCollections;
        private GUIContent[] availableCollectionsGUIContents;

        private bool singleCollection;
        private CollectionsDropDown collectionsDropdown;

        ~CollectionItemItemObjectPropertyDrawer()
        {
            if(item.IsNull())
                return;

            ObjectUtility.SetDirty(item);
        }
        
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
            if (singleCollection || OptionsAttribute.DrawType == DrawType.AsReference)
                return base.GetPropertyHeight(property, label);
            
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;
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

            Rect popupRect = position;
            popupRect.height = 15;

            using (new EditorGUI.PropertyScope(position, label, property))
            {
                popupRect = EditorGUI.PrefixLabel(popupRect, label);

                int indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;


                if (!singleCollection)
                {
                    DrawCollectionDropDown(ref popupRect, property);
                    popupRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }
                
                if (item != null)
                {
                    DrawEditFoldoutButton(ref popupRect);
                    DrawGotoButton(ref popupRect);
                }
                
                DrawCollectionItemDropDown(ref popupRect, property);

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
                EditorGUI.indentLevel = indent;
            }
        }

        private void DrawCollectionDropDown(ref Rect popupRect, SerializedProperty property)
        {
            int index = 0;
            if (collection != null)
                index = Array.IndexOf(availableCollections, collection) + 1;

            if (GUI.Button(popupRect, availableCollectionsGUIContents[index], EditorStyles.popup))
            {
                collectionsDropdown.Show(popupRect, objectCollection =>
                    {
                        OnCollectionChanged(objectCollection, property);
                    }
                );
            }
        }

        private void OnCollectionChanged(ScriptableObjectCollection scriptableObjectCollection, SerializedProperty property)
        {
            if (scriptableObjectCollection == null)
            {
                collection = null;
                property.objectReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
                return;
            }

            collection = scriptableObjectCollection;
            options = collection.Items.ToArray();
            List<string> displayOptions = collection.Items.Select(o => o.name).ToList();
            displayOptions.Insert(0, CollectionEditorGUI.DEFAULT_NONE_ITEM_TEXT);
            
            string[] optionsNames = displayOptions.ToArray();
            guiContents = optionsNames.Select(s => new GUIContent(s)).ToArray();
            collectionItemDropdown = new CollectionItemDropdown(new AdvancedDropdownState(), collection);
        }

        private void Initialize(SerializedProperty property)
        {
            if (initialized)
                return;
            Type arrayOrListType = fieldInfo.FieldType.GetArrayOrListType();
            Type itemType = arrayOrListType != null ? arrayOrListType : fieldInfo.FieldType;
            
            availableCollections = CollectionsRegistry.Instance.GetCollectionsByItemType(itemType).ToArray();
            if (availableCollections.Length == 0)
            {
                OptionsAttribute.DrawType = DrawType.AsReference;
                return;
            }
            
            if (availableCollections.Length == 1)
            {
                singleCollection = true;
                OnCollectionChanged(availableCollections.First(), property);
            }
            else
            {
                List<GUIContent> collectionsNames = availableCollections
                    .Select(objectCollection => new GUIContent(objectCollection.name))
                    .ToList();
                
                collectionsNames.Insert(0, new GUIContent("None"));
                availableCollectionsGUIContents = collectionsNames.ToArray();

                collectionsDropdown = new CollectionsDropDown(new AdvancedDropdownState(), availableCollections);

                if (property.objectReferenceValue != null)
                {
                    if (property.objectReferenceValue is ScriptableObjectCollectionItem collectionItem)
                    {
                        OnCollectionChanged(collectionItem.Collection, property);
                    }
                }
            }

            currentObject = property.serializedObject.targetObject;
            initialized = true;
        }

        private void DrawCollectionItemDropDown(ref Rect position, SerializedProperty property)
        {
            bool guiEnabled = GUI.enabled;
            if (collection == null)
                GUI.enabled = false;
            GUIContent displayValue = new GUIContent("Select Collection First");

            if (collection != null)
            {
                int selectedIndex = Array.IndexOf(options, item) + 1;
                displayValue = guiContents[selectedIndex];
            }
            
            if (GUI.Button(position, displayValue, EditorStyles.popup))
            {
                collectionItemDropdown.Show(position, o =>
                {
                    property.objectReferenceValue = o;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }

            GUI.enabled = guiEnabled;
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
