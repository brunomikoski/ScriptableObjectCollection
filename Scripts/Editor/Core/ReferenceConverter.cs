#if SOC_ADDRESSABLES
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    internal static class ReferenceConverter
    {
        private const string REFERENCE_QUALIFIED_CLASS_KEY = "SessionReferenceAssemblyQualifiedClassName";
        private const string BASE_CLASS_KEY = "SessionBaseClassName";
        internal const string BaseClassNamePostFix = "Base";
        internal static void StartProcess(ReorderableList reorderableList)
        {
            try
            {
                StartProcessInternal(reorderableList);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to convert items to references! {e}");
                EditorUtility.ClearProgressBar();
                SessionState.SetBool(CollectionCustomEditor.ENABLE_GUI_KEY, true);
                throw;
            }
        }
        internal static void StartProcessInternal(ReorderableList reorderableList)
        {
            if (reorderableList.count == 0) throw new Exception("Collection is empty!");
            ShowProgressBar("Reloading collections...", 0.1f);
            CollectionsRegistry.Instance.ReloadCollections();
            string referenceClassQualifiedName;
            string baseClassName;
            AssetDatabase.StartAssetEditing();
            try
            {
                ShowProgressBar("Creating base and reference class...", 0.2f);
                baseClassName = CreateItemBaseClass(reorderableList);
                SerializedProperty property = reorderableList.serializedProperty.GetArrayElementAtIndex(0);
                referenceClassQualifiedName = CreateReferenceClass(baseClassName, property);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            SessionState.SetString(REFERENCE_QUALIFIED_CLASS_KEY, referenceClassQualifiedName);
            SessionState.SetString(BASE_CLASS_KEY, baseClassName);
            // Player scripts recompilation is required to be able to use the newly created classes
            ShowProgressBar("Recompiling player scripts...", 0.3f);
            CompilationPipeline.RequestScriptCompilation();
        }
        
        private static string CreateItemBaseClass(ReorderableList reorderableList)
        {
            if (reorderableList.count == 0) throw new Exception("Collection is empty!");
            SerializedProperty property = reorderableList.serializedProperty.GetArrayElementAtIndex(0);
            string classNamespace = property.objectReferenceValue.GetType().Namespace;
            (string scriptPath, string scriptName) = GetAssociatedScriptMetaData(new SerializedObject(property.objectReferenceValue));
            string parentFolder = Directory.GetParent(scriptPath)?.ToString();
            string baseClassName = $"{scriptName}{BaseClassNamePostFix}";
            
            string baseClassPath = $"{parentFolder}/{baseClassName}.cs";
            if (File.Exists(baseClassPath)) File.Delete(baseClassPath);
            bool result = CodeGenerationUtility.CreateNewEmptyScript(baseClassName,
                parentFolder,
                classNamespace,
                "",
                $"public abstract class {baseClassName} : {nameof(ScriptableObjectCollectionItem)}", 
                null, 
                property.objectReferenceValue.GetType().Namespace, "BrunoMikoski.ScriptableObjectCollections");
            if (!result)
            {
                throw new Exception($"Failed to create item base class \"{baseClassName}.cs\"!");
            }

            return baseClassName;
        }
        
        private static string CreateReferenceClass(string baseClassName, SerializedProperty property)
        {
            string classNamespace = property.objectReferenceValue.GetType().Namespace;
            (string scriptPath, string scriptName) = GetAssociatedScriptMetaData(new SerializedObject(property.objectReferenceValue));
            string parentFolder = Directory.GetParent(scriptPath)?.ToString();
            string itemClassName = scriptName;
            string className = $"{itemClassName}Reference";
            
            string referenceClassPath = $"{parentFolder}/{className}.cs";
            if (File.Exists(referenceClassPath)) File.Delete(referenceClassPath);
            bool result = CodeGenerationUtility.CreateNewEmptyScript(className,
                parentFolder,
                classNamespace,
                "",
                $"public class {className} : {baseClassName}", 
                CodeGenerationUtility.ReferenceClassCode(itemClassName),
                property.objectReferenceValue.GetType().Namespace, "BrunoMikoski.ScriptableObjectCollections", 
                "UnityEngine", "System.Threading.Tasks");
            if (!result)
            {
                throw new Exception($"Failed to create item base class \"{className}.cs\"!");
            }

            if (!string.IsNullOrEmpty(classNamespace))
            {
                itemClassName = $"{classNamespace}.{itemClassName}";
                className = $"{classNamespace}.{className}";
            }

            string qualifiedName = property.objectReferenceValue.GetType().AssemblyQualifiedName;
            string[] typeProperties = qualifiedName?.Split(',');
            if (typeProperties != null && typeProperties.Length >= 1) typeProperties[0] = typeProperties[0].Replace(itemClassName, className);
            return String.Join(",", typeProperties);
        }

        [DidReloadScripts]
        private static async void OnScriptReload()
        {
            string referenceClassName = SessionState.GetString(REFERENCE_QUALIFIED_CLASS_KEY, string.Empty);
            string baseClassName = SessionState.GetString(BASE_CLASS_KEY, string.Empty);
            if (String.IsNullOrEmpty(referenceClassName) || String.IsNullOrEmpty(baseClassName)) return;
            SessionState.EraseString(REFERENCE_QUALIFIED_CLASS_KEY);
            SessionState.EraseString(BASE_CLASS_KEY);

            while (CollectionCustomEditor.editorInstance == null)
            {
                await Task.Delay(100);
            }
            EditorApplication.delayCall += () =>
                ResumeReferencesConversion(CollectionCustomEditor.editorInstance, referenceClassName, baseClassName);
        }

        private static void ResumeReferencesConversion(CollectionCustomEditor collectionEditor, string referenceClassQualifiedName, string baseClassName)
        {
            try
            {
                ConvertToReferenceItems(collectionEditor, referenceClassQualifiedName, baseClassName);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to convert items to references! {e}");
                throw;
            }
            finally
            {
                SessionState.SetBool(CollectionCustomEditor.ENABLE_GUI_KEY, true);
                EditorUtility.ClearProgressBar();
            }
        }
        
        private static void ConvertToReferenceItems(CollectionCustomEditor collectionEditor, string referenceClassQualifiedName, string baseClassName)
        {
            ShowProgressBar("Creating references...", 0.6f);
            SerializedProperty itemProperty =
                collectionEditor.ReorderableList.serializedProperty.GetArrayElementAtIndex(0);
            string actualItemType = itemProperty.objectReferenceValue.GetType().ToString();
            AssetDatabase.StartAssetEditing();
            try
            {
                UpdateInheritanceOfItemClass(collectionEditor.ReorderableList);
                ConvertToReferencesItemsInternal(collectionEditor.ReorderableList, referenceClassQualifiedName);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            ShowProgressBar("Regenerating collection and static class...", 0.8f);
            AssetDatabase.SaveAssets();
            collectionEditor.ReorderableList.serializedProperty.serializedObject.ApplyModifiedProperties();

            UpdateInheritanceOfCollectionClass(baseClassName, actualItemType, collectionEditor);
            CodeGenerationUtility.GenerateStaticCollectionScript(collectionEditor.Collection);
            
            ShowProgressBar("Conversion Complete!", 1.0f);
            EditorUtility.ClearProgressBar();
        }

        private static void UpdateInheritanceOfItemClass(ReorderableList reorderableList)
        {
            SerializedProperty property = reorderableList.serializedProperty.GetArrayElementAtIndex(0);
            (string scriptPath, string scriptName) = GetAssociatedScriptMetaData(new SerializedObject(property.objectReferenceValue));
            string className = scriptName;
            string baseClassName = $"{scriptName}{BaseClassNamePostFix}";
            string regexPattern =
                @$"(class\s+{className}\s+:\s+)({nameof(ScriptableObjectCollectionItem)})";
            string updatedFileContents = new Regex(regexPattern).Replace(File.ReadAllText(scriptPath), $"$1{baseClassName}", 1);
            File.WriteAllText(scriptPath, updatedFileContents);
        }

        private static (string path, string name) GetAssociatedScriptMetaData(SerializedObject itemObject)
        {
            UnityEngine.Object scriptProperty = itemObject.FindProperty("m_Script").objectReferenceValue;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(scriptProperty, out string scriptGuid, out long _))
                throw new Exception($"Failed to convert to references. This collection does not have an associated script!");
            return (AssetDatabase.GUIDToAssetPath(scriptGuid), scriptProperty.name);
        }

        private static void UpdateInheritanceOfCollectionClass(string baseClassName, string actualItemType, CollectionCustomEditor collectionEditor)
        {
            (string scriptPath, string scriptName) = GetAssociatedScriptMetaData(collectionEditor.serializedObject);
            string fileName = Path.GetFileNameWithoutExtension(scriptPath);
            string className = scriptName;
            string regexPattern =
                @$"(class\s+{className}\s+:\s+{nameof(ScriptableObjectCollection)})(<{actualItemType}>)";
            
            string updatedFileContents = new Regex(regexPattern).Replace(File.ReadAllText(scriptPath), $"$1<{baseClassName}>", 1);
            if (!updatedFileContents.Contains(baseClassName))
            {
                Debug.Log($"Could not match regex pattern to substitute base class of the collection {fileName}! Recreating collection class..");
                RecreateCollectionClass(baseClassName, collectionEditor.Collection, collectionEditor.serializedObject);
                return;
            }
            File.WriteAllText(scriptPath, updatedFileContents);
        }
        
        
        private static void RecreateCollectionClass(string baseClassName, ScriptableObjectCollection collection, SerializedObject serializedObject)
        {
            string classNamespace = collection.GetType().Namespace;
            (string scriptPath, string _) = GetAssociatedScriptMetaData(serializedObject);
            string parentFolder = Directory.GetParent(scriptPath)?.ToString();
            string fileName = Path.GetFileNameWithoutExtension(scriptPath);
            
            File.Delete(scriptPath);
            bool result = CodeGenerationUtility.CreateNewEmptyScript(fileName,
                parentFolder,
                classNamespace,
                $"[CreateAssetMenu(menuName = \"ScriptableObject Collection/Collections/Create {fileName}\", fileName = \"{fileName}\", order = 0)]",
                $"public class {fileName} : ScriptableObjectCollection<{baseClassName}>", 
                null, 
                typeof(ScriptableObjectCollection).Namespace, "UnityEngine", "System.Collections.Generic");
            if (!result)
            {
                throw new Exception($"Failed to create collection class \"{collection.name}.cs\"!");
            }
        }
        
        private static void ConvertToReferencesItemsInternal(ReorderableList reorderableList, string referenceClassName)
        {
            Type referenceType = Type.GetType(referenceClassName);
            for (int index = 0; index < reorderableList.count; index++)
            {
                SerializedProperty property = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
                ScriptableObjectCollectionItem collectionItem =
                    property.objectReferenceValue as ScriptableObjectCollectionItem;

                if (collectionItem == null || collectionItem.IsReference())
                    continue;
                string collectionPath = AssetDatabase.GetAssetPath(collectionItem.Collection);
                DirectoryInfo baseDirectory = Directory.GetParent(collectionPath);
                if (baseDirectory == null)
                {
                    Debug.LogWarning($"The collection path \"{collectionPath}\" for the item \"{collectionItem.name}\" is invalid! " +
                                     $"Skipping conversion for this item");
                    continue;
                }

                var item = (ScriptableObjectCollectionItem)ScriptableObject.CreateInstance(referenceType);
                item.name = $"{collectionItem.name}Reference";
                item.SetCollection(collectionItem.Collection);
                if (!item.TryGetReference(out var referenceItem))
                    throw new Exception("Something went wrong!");
                referenceItem.targetGuid = collectionItem.GUID;
                
                string referencesDirectory = Path.Combine(baseDirectory.ToString(), "References");
                Directory.CreateDirectory(referencesDirectory);
                AssetDatabase.CreateAsset(item, Path.Combine(referencesDirectory, $"{item.name}.asset"));
                
                CreateAddressablesGroups(collectionItem);
                property.objectReferenceValue = item;
                property.serializedObject.ApplyModifiedProperties();
            }
        }
        
        private static void CreateAddressablesGroups(ScriptableObjectCollectionItem collectionItem)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetEntry entry = settings.FindAssetEntry(collectionItem.GUID);
            if (entry == null)
            {
                string collectionName = collectionItem.Collection.name;
                AddressableAssetGroup assetGroup = settings.FindGroup(collectionName) ??
                                                   settings.CreateGroup(collectionName, false, false, false, null,
                                                       typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));
                assetGroup.GetSchema<ContentUpdateGroupSchema>().StaticContent = true;
                settings.CreateOrMoveEntry(collectionItem.GUID, assetGroup);
                EditorUtility.SetDirty(assetGroup);
            }
        }
        
        private static void ShowProgressBar(string state, float progress)
        {
            EditorUtility.DisplayProgressBar("Converting to references...", state, progress);
            Debug.Log(state);
        }
    }
}
#endif