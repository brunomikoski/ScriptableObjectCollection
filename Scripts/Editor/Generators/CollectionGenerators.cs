using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        private static IScriptableObjectCollectionGeneratorBase GetGenerator(Type type)
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
        
        [MenuItem("SOC/Generate All", false, int.MaxValue)]
        public static void GenerateAll()
        {
            // Find all the generator types.
            Type generatorInterface = typeof(IScriptableObjectCollectionGenerator<,>);
            Type[] generatorTypes = generatorInterface.GetAllAssignableClasses();
            
            foreach (Type generatorType in generatorTypes)
            {
                GenerateCollectionInternal(generatorType, false);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void GenerateCollection(Type generatorType)
        {
            GenerateCollectionInternal(generatorType, true);
        }

        private static void GenerateCollectionInternal(Type generatorType, bool refresh)
        {
            Type generatorInterface = typeof(IScriptableObjectCollectionGenerator<,>);
            
            // Figure out what type of generator this is.
            Type interfaceType = generatorType.GetInterface(generatorInterface.Name);
            Type[] genericArguments = interfaceType.GetGenericArguments();
            Type collectionType = genericArguments[0];
            Type itemTemplateType = genericArguments[1];
            
            // Check that the corresponding collection exists.
            CollectionsRegistry.Instance.TryGetCollectionOfType(
                collectionType, out ScriptableObjectCollection collection);
            if (collection == null)
            {
                Debug.LogWarning(
                    $"Tried to generate items for collection '{collectionType.Name}' but no such " +
                    $"collection existed.");
                return;
            }

            // Get an instance of the generator.
            IScriptableObjectCollectionGeneratorBase generator = GetGenerator(generatorType);

            // Make an empty list that will hold the generated item templates.
            Type genericListType = typeof(List<>);
            Type templateListType = genericListType.MakeGenericType(itemTemplateType);
            IList list = (IList)Activator.CreateInstance(templateListType);

            // Make the generator generate item templates.
            MethodInfo getItemTemplatesMethod = generatorType.GetMethod(
                "GetItemTemplates", BindingFlags.Public | BindingFlags.Instance);
            getItemTemplatesMethod.Invoke(generator, new object[] {list, collection});
            
            // If necessary, first remove any items that weren't re-generated.
            bool shouldRemoveNonGeneratedItems = (bool)generatorType
                .GetProperty("ShouldRemoveNonGeneratedItems", BindingFlags.Public | BindingFlags.Instance)
                .GetValue(generator);
            if (shouldRemoveNonGeneratedItems)
            {
                for (int i = collection.Items.Count - 1; i >= 0; i--)
                {
                    bool didHaveTemplateItemWithSameName = false;
                    for (int j = 0; j < list.Count; j++)
                    {
                        ItemTemplate itemTemplate = (ItemTemplate)list[j];
                        if (collection.Items[i].name == itemTemplate.name)
                        {
                            didHaveTemplateItemWithSameName = true;
                            break;
                        }
                    }
                    
                    if (!didHaveTemplateItemWithSameName)
                    {
                        // No corresponding template existed, so remove this item.
                        ScriptableObject itemToRemove = collection.Items[i];
                        collection.RemoveAt(i);
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(itemToRemove));
                    }
                }
            }
            
            // Now try to find or create corresponding items in the collection and copy the fields over.
            for (int i = 0; i < list.Count; i++)
            {
                ItemTemplate itemTemplate = (ItemTemplate)list[i];

                if (itemTemplate == null)
                    continue;

                ISOCItem itemInstance = collection.GetOrAddNewBaseItem(itemTemplate.name);
                CopyFieldsFromTemplateToItem(itemTemplate, itemInstance);
            }

            if (refresh)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
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
            SerializedProperty serializedProperty = serializedObject.FindProperty(field.Name);
            if (serializedProperty == null)
                return;
            
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
