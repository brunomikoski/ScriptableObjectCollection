using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

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
            
            // Now try to find or create corresponding items in the collection and copy the fields over.
            foreach (object item in list)
            {
                ItemTemplate itemTemplate = (ItemTemplate)item;

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
                object value = field.GetValue(itemTemplate);
                CopyFieldToSerializedProperty(field, value, serializedObject);
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        private static void CopyFieldToSerializedProperty(FieldInfo fieldInfo, object value, SerializedObject serializedObject)
        {
            // Make sure the field is serializable.
            if (fieldInfo.IsPrivate && fieldInfo.GetCustomAttribute<SerializeField>() == null)
                return;

            // Get the property to copy the value to.
            SerializedProperty serializedProperty = serializedObject.FindProperty(fieldInfo.Name);
            if (serializedProperty == null)
                return;

            switch (serializedProperty.propertyType)
            {
                case SerializedPropertyType.Integer:
                    serializedProperty.intValue = (int)value;
                    break;
                case SerializedPropertyType.Boolean:
                    serializedProperty.boolValue = (bool)value;
                    break;
                case SerializedPropertyType.Float:
                    serializedProperty.floatValue = (float)value;
                    break;
                case SerializedPropertyType.String:
                    serializedProperty.stringValue = (string)value;
                    break;
                case SerializedPropertyType.Color:
                    serializedProperty.colorValue = (Color)value;
                    break;
                case SerializedPropertyType.ObjectReference:
                    serializedProperty.objectReferenceValue = (UnityEngine.Object)value;
                    break;
                case SerializedPropertyType.LayerMask:
                    serializedProperty.intValue = (LayerMask)value;
                    break;
                case SerializedPropertyType.Enum:
                    serializedProperty.intValue = (int)value;
                    break;
                case SerializedPropertyType.Vector2:
                    serializedProperty.vector2Value = (Vector2)value;
                    break;
                case SerializedPropertyType.Vector3:
                    serializedProperty.vector3Value = (Vector3)value;
                    break;
                case SerializedPropertyType.Vector4:
                    serializedProperty.vector4Value = (Vector4)value;
                    break;
                case SerializedPropertyType.Rect:
                    serializedProperty.rectValue = (Rect)value;
                    break;
                //case SerializedPropertyType.ArraySize:
                    //break;
                case SerializedPropertyType.Character:
                    serializedProperty.stringValue = ((char)value).ToString();
                    break;
                case SerializedPropertyType.AnimationCurve:
                    serializedProperty.animationCurveValue = (AnimationCurve)value;
                    break;
                case SerializedPropertyType.Bounds:
                    serializedProperty.boundsValue = (Bounds)value;
                    break;
                //case SerializedPropertyType.Gradient:
                    //break;
                case SerializedPropertyType.Quaternion:
                    serializedProperty.quaternionValue = (Quaternion)value;
                    break;
                //case SerializedPropertyType.ExposedReference:
                    //break;
                //case SerializedPropertyType.FixedBufferSize:
                    //break;
                case SerializedPropertyType.Vector2Int:
                    serializedProperty.vector2IntValue = (Vector2Int)value;
                    break;
                case SerializedPropertyType.Vector3Int:
                    serializedProperty.vector3IntValue = (Vector3Int)value;
                    break;
                case SerializedPropertyType.RectInt:
                    serializedProperty.rectIntValue = (RectInt)value;
                    break;
                case SerializedPropertyType.BoundsInt:
                    serializedProperty.boundsIntValue = (BoundsInt)value;
                    break;
                //case SerializedPropertyType.ManagedReference:
                    //break;
                //case SerializedPropertyType.Hash128:
                    //break;
                case SerializedPropertyType.Generic:
                default:
                    Debug.LogWarning($"Tried to copy value '{value}' from a template to an SOC item but apparently that's not supported.");
                    break;
            }
            
        }
    }
}
