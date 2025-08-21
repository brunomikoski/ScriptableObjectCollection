using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BrunoMikoski.ScriptableObjectCollections.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Responsible for managing collection generators.
    /// </summary>
    public static class CollectionGenerators 
    {
        private static readonly Dictionary<Type, IScriptableObjectCollectionGeneratorBase> generatorTypeToInstance 
            = new Dictionary<Type, IScriptableObjectCollectionGeneratorBase>();
        
        private static Type InterfaceType => typeof(IScriptableObjectCollectionGenerator<,>);
        
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // Make sure we clean this when code reloads.
            generatorTypeToInstance.Clear();
        }

        public static IScriptableObjectCollectionGeneratorBase GetGenerator(Type type)
        {
            bool existed = generatorTypeToInstance.TryGetValue(
                type, out IScriptableObjectCollectionGeneratorBase instance);
            if (!existed)
            {
                instance = (IScriptableObjectCollectionGeneratorBase)Activator.CreateInstance(type);
                generatorTypeToInstance.Add(type, instance);
            }

            return instance;
        }
        
        private static void GetGeneratorTypes(Type generatorType, out Type collectionType, out Type templateType)
        {
            Type interfaceType = generatorType.GetInterface(InterfaceType.Name);
            Type[] genericArguments = interfaceType.GetGenericArguments();
            collectionType = genericArguments[0];
            templateType = genericArguments[1];
        }
        
        private static Type[] GetAllGeneratorTypes()
        {
            return InterfaceType.GetAllAssignableClasses();
        }

        public static Type GetGeneratorTypeForCollection(Type collectionType, bool allowSubclasses = true)
        {
            Type[] generatorTypes = GetAllGeneratorTypes();
            foreach (Type generatorType in generatorTypes)
            {
                GetGeneratorTypes(generatorType, out Type generatorCollectionType, out Type generatorTemplateType);
                if (generatorCollectionType == collectionType || collectionType.IsSubclassOf(generatorCollectionType))
                    return generatorType;
            }

            return null;
        }
        
        public static void RunAllGenerators()
        {
            Type[] generatorTypes = GetAllGeneratorTypes();
            foreach (Type generatorType in generatorTypes)
            {
                RunGeneratorInternal(generatorType, false);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        public static void RunGenerator(Type generatorType, ScriptableObjectCollection targetCollection = null, bool generateStaticAccess = false)
        {
            IScriptableObjectCollectionGeneratorBase generator = GetGenerator(generatorType);

            RunGeneratorInternal(generator, targetCollection, true, generateStaticAccess);        
        }
        
        public static void RunGenerator(Type generatorType, bool generateStaticAccess = false)
        {
            RunGeneratorInternal(generatorType, true, generateStaticAccess);
        }
        
        public static void RunGenerator<GeneratorType>(bool generateStaticAccess = false)
            where GeneratorType : IScriptableObjectCollectionGeneratorBase
        {
            RunGenerator(typeof(GeneratorType), generateStaticAccess);
        }
        
        public static void RunGenerator(
            IScriptableObjectCollectionGeneratorBase generator, bool generateStaticAccess = false)
        {
            RunGeneratorInternal(generator, null, true, generateStaticAccess);
        }

        private static void RunGeneratorInternal(Type generatorType, bool refresh, bool generateStaticAccess = false)
        {
            IScriptableObjectCollectionGeneratorBase generator = GetGenerator(generatorType);

            RunGeneratorInternal(generator, null, refresh, generateStaticAccess);
        }

        private static void RunGeneratorInternal(
            IScriptableObjectCollectionGeneratorBase generator, ScriptableObjectCollection collection, bool refresh, bool generateStaticAccess)
        {
            Type generatorType = generator.GetType();
            
            GetGeneratorTypes(generatorType, out Type collectionType, out Type itemTemplateType);

            if (collection == null)
            {
                // Check that the corresponding collection exists.
                CollectionsRegistry.Instance.TryGetCollectionOfType(
                    collectionType, out collection);
                if (collection == null)
                {
                    Debug.LogWarning(
                        $"Tried to generate items for collection '{collectionType.Name}' but no such " +
                        $"collection existed.");
                    return;
                }
            }

            // Make an empty list that will hold the generated item templates.
            Type genericListType = typeof(List<>);
            Type templateListType = genericListType.MakeGenericType(itemTemplateType);
            IList templates = (IList)Activator.CreateInstance(templateListType);

            // Make the generator generate item templates.
            MethodInfo getItemTemplatesMethod = generatorType.GetMethod(
                "GetItemTemplates", BindingFlags.Public | BindingFlags.Instance);
            getItemTemplatesMethod.Invoke(generator, new object[] {templates, collection});

            // If necessary, first remove any items that weren't re-generated.
            bool shouldRemoveNonGeneratedItems = (bool)generatorType
                .GetProperty("ShouldRemoveNonGeneratedItems", BindingFlags.Public | BindingFlags.Instance)
                .GetValue(generator);
            if (shouldRemoveNonGeneratedItems)
            {
                for (int i = collection.Items.Count - 1; i >= 0; i--)
                {
                    // Remove any items for which there isn't a template by the same name.
                    bool foundItemOfSameName = false;
                    for (int j = 0; j < templates.Count; j++)
                    {
                        ItemTemplate itemTemplate = (ItemTemplate)templates[j];
                        if (collection.Items[i].name == itemTemplate.name)
                        {
                            foundItemOfSameName = true;
                            break;
                        }
                    }
                    if (!foundItemOfSameName)
                    {
                        // No corresponding template existed, so remove this item.
                        ScriptableObject itemToRemove = collection.Items[i];
                        collection.RemoveAt(i);
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(itemToRemove));
                    }
                }
            }
            
            // Now try to find or create corresponding items in the collection and copy the fields over.
            for (int i = 0; i < templates.Count; i++)
            {
                ItemTemplate itemTemplate = (ItemTemplate)templates[i];

                if (itemTemplate == null)
                    continue;


                if (!TryGetItemTemplateType(itemTemplate, out Type templateItemType))
                    templateItemType = collection.GetItemType();
                
                ISOCItem itemInstance = collection.GetOrAddNew(templateItemType, itemTemplate.name);

                CopyFieldsFromTemplateToItem(itemTemplate, itemInstance);
            }
            
            
            // Optional Callback to be called when the generation completes
            MethodInfo completionCallback = generatorType.GetMethod(
                "OnItemsGenerationComplete", BindingFlags.Public | BindingFlags.Instance);
            if (completionCallback != null)
                completionCallback!.Invoke(generator, new object[] {collection });
            

            if (refresh)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            
            if (generateStaticAccess)
                CodeGenerationUtility.GenerateStaticCollectionScript(collection);

            EditorUtility.SetDirty(collection);
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        private static bool TryGetItemTemplateType(ItemTemplate itemTemplate, out Type resultType)
        {
            Type itemType = GetGenericItemType(itemTemplate);
            if (itemType == null)
            {
                resultType = null;
                return false;
            }
            
            resultType = itemType.GetGenericArguments().First();
            return resultType != null;
        }

        public static Type GetTemplateItemType(ItemTemplate itemTemplate)
        {
            Type itemType = GetGenericItemType(itemTemplate);
            if (itemType == null)
                return null;
            
            Type genericType = itemType.GetGenericArguments().First();
            return genericType;
        }

        private static Type GetGenericItemType(ItemTemplate itemTemplate)
        {
            Type baseType = itemTemplate.GetType().BaseType;

            while (baseType != null)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(ItemTemplate<>))
                    return baseType;
                baseType = baseType.BaseType;
            }
            return null;
        }

        private static void CopyFieldsFromTemplateToItem(ItemTemplate itemTemplate, ISOCItem itemInstance)
        {
            SerializedObject serializedObject = new SerializedObject(itemInstance as ScriptableObject);
            serializedObject.Update();
            
            Type itemTemplateType = itemTemplate.GetType();
            FieldInfo[] fields = itemTemplateType.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (FieldInfo field in fields)
            {
                CopyFieldToSerializedProperty(field, itemTemplate, serializedObject);
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        private static void CopyFieldToSerializedProperty(
            FieldInfo field, object owner, SerializedObject serializedObject)
        {
            // Make sure the field is serializable.
            if (field.IsPrivate && field.GetCustomAttribute<SerializeField>() == null)
                return;

            // Get the property to copy the value to.
            serializedObject.TryFindProperty(field.Name, out SerializedProperty serializedProperty);
            
            object value = field.GetValue(owner);
            
            // Support arrays.
            if (serializedProperty.isArray && serializedProperty.propertyType == SerializedPropertyType.Generic)
            {
                IEnumerable<object> collection = (IEnumerable<object>)value;
                serializedProperty.arraySize = collection.Count();
                int index = 0;
                foreach (object arrayItem in collection)
                {
                    SerializedProperty arrayElement = serializedProperty.GetArrayElementAtIndex(index);
                    arrayElement.SetValue(arrayItem);
                    index++;
                }
                return;
            }
            
            serializedProperty.SetValue(value);
        }
    }
}
