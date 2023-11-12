using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Browser
{
    [Serializable]
    public class BrowserSettings
    {
        private const string PATH = "UserSettings/ScriptableObjectCollectionBrowser.json";

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Scriptable Object Collection/Browser", SettingsScope.Project)
            {
                label = "Browser",
                guiHandler = Instance.OnGUI,
                keywords = new string[] { "SOC", "Scriptable Objects", "Scriptable Objects Collection", "Browser" }
            };
        }

        private static BrowserSettings instance;

        public static BrowserSettings Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                if (File.Exists(PATH))
                {
                    string json = File.ReadAllText(PATH);
                    instance = JsonUtility.FromJson<BrowserSettings>(json);
                }
                else
                {
                    instance = new BrowserSettings();
                }

                return instance;
            }
        }

        [SerializeField] private List<string> serializedTypesToIgnore = new();
        [SerializeField] private bool showHiddenCollections;
        [NonSerialized] private List<Type> uiTypesToIgnore = new();
        [NonSerialized] private TypeCache.TypeCollection cachedDerivedTypes;
        [NonSerialized] private bool hasCachedDerivedTypes;

        private ReorderableList reorderableList;

        public TypeCache.TypeCollection DerivedTypes
        {
            get
            {
                if (hasCachedDerivedTypes)
                    return cachedDerivedTypes;

                cachedDerivedTypes = TypeCache.GetTypesDerivedFrom<ScriptableObjectCollection>();
                hasCachedDerivedTypes = true;
                return cachedDerivedTypes;
            }
        }

        public bool ShowHiddenCollections
        {
            get => showHiddenCollections;
            set
            {
                showHiddenCollections = value;
                Save();
            }
        }

        public event Action SettingsChanged;

        public bool IsHiddenCollection(Type type)
        {
            return serializedTypesToIgnore.Contains(type.AssemblyQualifiedName);
        }

        private void OnGUI(string searchContext)
        {
            if (reorderableList == null)
            {
                InitializeList();
            }

            EditorGUI.BeginChangeCheck();
            showHiddenCollections = EditorGUILayout.Toggle("Show Hidden Collections", showHiddenCollections);
            if (EditorGUI.EndChangeCheck())
            {
                Save();
            }

            EditorGUILayout.LabelField("Collections to hide", EditorStyles.boldLabel);
            reorderableList.DoLayoutList();
        }

        private void InitializeList()
        {
            uiTypesToIgnore = serializedTypesToIgnore.Select(Type.GetType).ToList();

            reorderableList = new ReorderableList(uiTypesToIgnore, typeof(Type), true, false, true, true);
            reorderableList.drawElementCallback += OnDrawElement;
            reorderableList.onAddCallback += OnAddElement;
            reorderableList.onRemoveCallback += OnRemoveElement;
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            GUI.Label(rect, uiTypesToIgnore[index].Name);
        }

        private void OnAddElement(ReorderableList list)
        {
            List<Type> items = GetFilteredTypes();
            GenericMenu genericMenu = new();

            foreach (Type type in items)
            {
                // We don't want to allow the user to remove the last collection, that breaks the tree view
                if (items.Count == 1)
                {
                    genericMenu.AddDisabledItem(new GUIContent(type.Name));
                }
                else
                {
                    genericMenu.AddItem(new GUIContent(type.Name),
                        false,
                        () =>
                        {
                            uiTypesToIgnore.Add(type);
                            serializedTypesToIgnore.Add(type.AssemblyQualifiedName);
                            Save();
                        });
                }
            }

            genericMenu.ShowAsContext();
        }

        private void OnRemoveElement(ReorderableList list)
        {
            uiTypesToIgnore.RemoveAt(list.index);
            serializedTypesToIgnore.RemoveAt(list.index);
            Save();
        }

        private void Save()
        {
            string json = EditorJsonUtility.ToJson(this, prettyPrint: true);
            File.WriteAllText(PATH, json);
            SettingsChanged?.Invoke();
        }

        public void ToggleCollection(ScriptableObjectCollection collection)
        {
            Type type = collection.GetType();

            if (serializedTypesToIgnore.Contains(type.AssemblyQualifiedName))
            {
                uiTypesToIgnore.Remove(type);
                serializedTypesToIgnore.Remove(type.AssemblyQualifiedName);
            }
            else
            {
                uiTypesToIgnore.Add(type);
                serializedTypesToIgnore.Add(type.AssemblyQualifiedName);
            }

            Save();
        }

        public bool CanHide(ScriptableObjectCollection collection)
        {
            if (serializedTypesToIgnore.Contains(collection.GetType().AssemblyQualifiedName))
                return true;

            List<Type> items = GetFilteredTypes();
            return items.Count > 1;
        }

        private List<Type> GetFilteredTypes()
        {
            List<Type> items = new();
            TypeCache.TypeCollection typesDerivedFrom = DerivedTypes;
            foreach (Type type in typesDerivedFrom)
            {
                if (type.IsAbstract)
                    continue;

                if (type.ContainsGenericParameters)
                    continue;

                if (uiTypesToIgnore.Contains(type))
                    continue;

                items.Add(type);
            }

            return items;
        }
    }
}
