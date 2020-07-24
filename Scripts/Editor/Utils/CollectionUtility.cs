using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class CollectionUtility
    {
        private const string DEFAULT_GENERATED_PATH = "Assets/Generated/Scripts";

        private const string SCRIPTS_FOLDER_KEY = "LastScriptsFolderKey";
        private const string SCRIPTABLE_OBJECT_FOLDER_KEY = "LastScriptableObjectFolderKey";
        private const string NAMESPACE_KEY = "LastNamespaceKey";
        private const string GENERATED_SCRIPTS_FOLDER_KEY = "GeneratedScriptsFolderKey";

        private static Dictionary<CollectableScriptableObject, Editor> itemToEditor =
            new Dictionary<CollectableScriptableObject, Editor>();

        private static Dictionary<Object, bool> objectToFoldOut = new Dictionary<Object, bool>();

        
        public static string ScriptableObjectFolderPath
        {
            get => EditorPrefs.GetString($"{Application.productName}{SCRIPTABLE_OBJECT_FOLDER_KEY}", String.Empty);
            set => EditorPrefs.SetString($"{Application.productName}{SCRIPTABLE_OBJECT_FOLDER_KEY}", value);
        }

        public static string ScriptsFolderPath
        {
            get => EditorPrefs.GetString($"{Application.productName}{SCRIPTS_FOLDER_KEY}", String.Empty);
            set => EditorPrefs.SetString($"{Application.productName}{SCRIPTS_FOLDER_KEY}", value);
        }

        public static string TargetNamespace
        {
            get => EditorPrefs.GetString($"{Application.productName}{NAMESPACE_KEY}", String.Empty);
            set => EditorPrefs.SetString($"{Application.productName}{NAMESPACE_KEY}", value);
        }
        
        public static string StaticGeneratedScriptsFolderPath
        {
            get => EditorPrefs.GetString($"{Application.productName}{GENERATED_SCRIPTS_FOLDER_KEY}", DEFAULT_GENERATED_PATH);
            set => EditorPrefs.SetString($"{Application.productName}{GENERATED_SCRIPTS_FOLDER_KEY}", value);
        }


        [MenuItem("Assets/Create/Scriptable Object Collection/New Collection", false, 100)]
        private static void CreateNewItem(MenuCommand menuCommand)
        {
            CreateCollectionWizzard.Show();
        }
        
        [MenuItem("Assets/Create/Scriptable Object Collection/Create Settings", false, 200)]
        private static void CreateSettings()
        {
            ScriptableObjectCollectionSettings.LoadOrCreateInstance();
        }
        
        public static Editor GetEditorForItem(CollectableScriptableObject collectionItem)
        {
            if (itemToEditor.TryGetValue(collectionItem, out Editor customEditor))
                return customEditor;
            
            Editor.CreateCachedEditor(collectionItem, null, ref customEditor);
            itemToEditor.Add(collectionItem, customEditor);
            return customEditor;
        }
        
        public static bool IsFoldoutOpen(Object targetObject)
        {
            if (targetObject.IsNull())
                return false;
            
            bool value;
            if(!objectToFoldOut.TryGetValue(targetObject, out value))
                objectToFoldOut.Add(targetObject, value);

            return value;
        }

        public static void SetFoldoutOpen(Object targetObject, bool value)
        {
            if (!objectToFoldOut.ContainsKey(targetObject))
                objectToFoldOut.Add(targetObject, value);
            else
                objectToFoldOut[targetObject] = value;
        }

       
    }
}

