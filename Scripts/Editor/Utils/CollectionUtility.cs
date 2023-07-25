using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class CollectionUtility
    {
        private static Dictionary<int, bool> objectToFoldOut = new Dictionary<int, bool>();

        private static int GetHasgCount(object[] objects)
        {
            int hashValue = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                object targetObj = objects[i];

                if (targetObj == null)
                    continue;

                hashValue += HashCode.Combine(hashValue, targetObj.GetHashCode());
            }

            return hashValue;
        }

        public static bool IsFoldoutOpen(params Object[] objects)
        {
            int hashCount = GetHasgCount(objects);
           
            if (hashCount == 0)
                return false;

            if(!objectToFoldOut.TryGetValue(hashCount, out bool value))
                objectToFoldOut.Add(hashCount, value);

            return value;
        }
        

        public static void SetCollectionItemExpanded(bool isExpanded, ISOCItem targetItem)
        {
            ScriptableObjectCollection collection = targetItem.Collection;
            SetCollectionItemExpanded(isExpanded, targetItem, collection);
        }

        public static bool IsCollectionItemExpanded(ISOCItem targetItem)
        {
            SerializedObject collectionSerializedObject = new SerializedObject(targetItem.Collection);
            SerializedProperty itemsProperty = collectionSerializedObject.FindProperty("items");
            for (int i = 0; i < itemsProperty.arraySize; i++)
            {
                SerializedProperty itemProperty = itemsProperty.GetArrayElementAtIndex(i);
                if (itemProperty.objectReferenceValue == (Object) targetItem)
                {
                    return itemProperty.isExpanded;
                }
            }

            return false;
        }

        public static bool IsCollectionItemExpanded(params object[] objects)
        {
            int hash = GetHasgCount(objects);
            if (objectToFoldOut.TryGetValue(hash, out bool isOpen))
                return isOpen;
            return false;
        }

        public static void SetCollectionItemExpanded(bool isExpanded, params object[] objects)
        {
            int hashCount = GetHasgCount(objects);
            objectToFoldOut[hashCount] = isExpanded;
        }

        public static void SetCollectionItemExpanded(bool isExpanded, ISOCItem collectionItem, ScriptableObjectCollection collection)
        {
            SerializedObject collectionSerializedObject = new SerializedObject(collection);
            SerializedProperty itemsProperty = collectionSerializedObject.FindProperty("items");
            for (int i = 0; i < itemsProperty.arraySize; i++)
            {
                SerializedProperty itemProperty = itemsProperty.GetArrayElementAtIndex(i);
                if (itemProperty.objectReferenceValue == (Object) collectionItem)
                {
                    itemProperty.isExpanded = isExpanded;
                    break;
                }
            }
        }

        public static void SetOnlyCollectionItemExpanded(ISOCItem collectionItem, ScriptableObjectCollection collection)
        {
            SerializedObject collectionSerializedObject = new SerializedObject(collection);
            SerializedProperty itemsProperty = collectionSerializedObject.FindProperty("items");
            for (int i = 0; i < itemsProperty.arraySize; i++)
            {
                SerializedProperty itemProperty = itemsProperty.GetArrayElementAtIndex(i);
                itemProperty.isExpanded = itemProperty.objectReferenceValue == (Object) collectionItem; 
            }
        }
    }
}

