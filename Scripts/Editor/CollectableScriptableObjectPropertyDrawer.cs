using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomPropertyDrawer(typeof(CollectableScriptableObject), true)]
    public class CollectableScriptableObjectPropertyDrawer : PropertyDrawer
    {
        private static readonly CollectableEditorOptionsAttribute defaultAttribute
            = new CollectableEditorOptionsAttribute(DrawType.Dropdown);

        private CollectableEditorOptionsAttribute cachedOptionsAttribute;

        private CollectableEditorOptionsAttribute optionsAttribute
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
        
        private CollectableScriptableObject[] options;
        private string[] optionsNames;
        private GUIContent[] GUIContents;
        
        private CollectableScriptableObject collectableItem;
        private Object currentObject;

        ~CollectableScriptableObjectPropertyDrawer()
        {
            if(collectableItem.IsNull())
                return;

            ObjectUtility.SetDirty(collectableItem);
        }
        
        private CollectableEditorOptionsAttribute GetOptionsAttribute()
        {
            if (fieldInfo == null)
                return defaultAttribute;
            object[] attributes
                = fieldInfo.GetCustomAttributes(typeof(CollectableEditorOptionsAttribute), false);
            if (attributes.Length > 0)
                return attributes[0] as CollectableEditorOptionsAttribute;
            return defaultAttribute;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Initialize(property);
            
            collectableItem = property.objectReferenceValue as CollectableScriptableObject;

            if (optionsAttribute.DrawType == DrawType.AsReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;  
            }
            
            
            Rect popupRect = position;
            popupRect.height = 15;

            if (collectableItem != null)
            {
                DrawEditFoldoutButton(ref popupRect);
                DrawGotoButton(collection, ref popupRect);
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

                        DrawDropDown(popupRect, property);

                        break;
                    }

                    default:
                        throw new ArgumentOutOfRangeException("DrawType: " + optionsAttribute.DrawType);
                }

                if (collectableItem != null)
                {
                    if (CollectionUtility.IsFoldoutOpen(collectableItem, currentObject))
                    {
                        EditorGUI.indentLevel++;
                        using (new EditorGUILayout.VerticalScope("Box"))
                        {
                            Editor editor = CollectionUtility.GetOrCreateEditorForItem(collectableItem);
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
            
            Type collectableType;
            Type arrayOrListType = fieldInfo.FieldType.GetArrayOrListType();
            if (arrayOrListType != null)
                collectableType = arrayOrListType;
            else
                collectableType = fieldInfo.FieldType;
            
            if (!CollectionsRegistry.Instance.TryGetCollectionForType(collectableType,
                out ScriptableObjectCollection resultCollection))
            {
                optionsAttribute.DrawType = DrawType.AsReference;
                return;
            }

            collection = resultCollection;

            options = collection.Items.ToArray();
            List<string> displayOptions = collection.Items.Select(o => o.name).ToList();
            displayOptions.Insert(0, CollectionEditorGUI.DEFAULT_NONE_ITEM_TEXT);
            
            optionsNames = displayOptions.ToArray();
            GUIContents = optionsNames.Select(s => new GUIContent(s)).ToArray();

            currentObject = property.serializedObject.targetObject;
            initialized = true;
        }

        private void DrawDropDown(Rect position, SerializedProperty property)
        {
            using (EditorGUI.ChangeCheckScope changedCheck = new EditorGUI.ChangeCheckScope())
            {
                int selectedIndex = 0;

                if (collectableItem != null)
                    selectedIndex = Array.IndexOf(options, collectableItem) + 1;


                int newSelectedIndex = EditorGUI.Popup(position, selectedIndex,
                    GUIContents, EditorStyles.popup);

                newSelectedIndex -= 1;

                collectableItem = newSelectedIndex >= 0 ? options[newSelectedIndex] : null;
                if (changedCheck.changed)
                {
                    property.objectReferenceValue = collectableItem;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }
        
        public static void DrawGotoButton(ScriptableObjectCollection enumValues, ref Rect popupRect)
        {
            Rect buttonRect = popupRect;
            buttonRect.width = 30;
            buttonRect.height = 18;
            popupRect.width -= buttonRect.width;
            buttonRect.x += popupRect.width;
            if (GUI.Button(buttonRect, CollectionEditorGUI.ARROW_RIGHT_CHAR))
            {
                Selection.activeObject = enumValues;
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
            if (CollectionUtility.IsFoldoutOpen(collectableItem, currentObject))
                guiContent = CollectionEditorGUI.CloseGUIContent;

            if (GUI.Button(buttonRect, guiContent))
            {
                CollectionUtility.SetFoldoutOpen(!CollectionUtility.IsFoldoutOpen(collectableItem, currentObject), collectableItem, currentObject);
                ObjectUtility.SetDirty(collectableItem);
            }
        }
    }
}
