using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class SerializedPropertyExtensions
    {
        public static FieldInfo GetFieldInfoFromPathByType(this SerializedProperty property, Type parentType)
        {
            string path = property.propertyPath;
            FieldInfo fieldInfo = parentType.GetField(path);
            string[] perDot = path.Split('.');
            foreach (string fieldName in perDot)
            {
                fieldInfo = parentType.GetField(fieldName);
                if (fieldInfo == null) 
                    return null;
                
                parentType = fieldInfo.FieldType;
            }
            
            return fieldInfo;
        }

        public static void SetValueReflective(this SerializedProperty prop, object value)
        {
            object root = prop.serializedObject.targetObject;

            string[] parts = prop.propertyPath
                .Replace(".Array.data[", "[")
                .Split('.');

            object current = root;
            FieldInfo fi = null;

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];

                if (part.EndsWith("]"))
                {
                    int b = part.IndexOf('[');
                    string name = part.Substring(0, b);
                    int idx = int.Parse(part.Substring(b + 1, part.Length - b - 2));

                    fi = current.GetType()
                        .GetField(name,
                            BindingFlags.Instance
                            | BindingFlags.NonPublic
                            | BindingFlags.Public);
                    IList list = fi.GetValue(current) as System.Collections.IList;
                    current = list[idx];
                }
                else
                {
                    fi = current.GetType()
                        .GetField(part,
                            BindingFlags.Instance
                            | BindingFlags.NonPublic
                            | BindingFlags.Public);
                    if (i < parts.Length - 1)
                        current = fi.GetValue(current);
                }
            }

            fi.SetValue(current, value);

            prop.serializedObject.Update();
            prop.serializedObject.ApplyModifiedProperties();
        }

        public static void SetValue(this SerializedProperty serializedProperty, object value) {
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
                case SerializedPropertyType.Hash128:
                    serializedProperty.hash128Value = (Hash128)value;
                break;
                case SerializedPropertyType.ManagedReference:
                    serializedProperty.managedReferenceValue = value;
                    break;
                case SerializedPropertyType.Generic:
                    serializedProperty.SetValueReflective(value);
                    break;
                default:
                    Debug.LogWarning(
                        $"Tried to copy value '{value}' from a template to an SOC item but apparently that's not supported.");
                    break;
            }
        }

        public static T GetActualObject<T>(this SerializedProperty property, FieldInfo fieldInfo)
            where T : class
        {
            string label = string.Empty;
            return property.GetActualObjectForSerializedProperty<T>(fieldInfo, ref label);
        }

        /// <summary>
        /// Used to extract the target object from a serialized property.
        /// NOTE: This implementation comes from Unity's own Addressables package.
        /// </summary>
        /// <typeparam name="T">The type of the object to extract.</typeparam>
        /// <param name="property">The property containing the object.</param>
        /// <param name="field">The field data.</param>
        /// <param name="label">The label name.</param>
        /// <returns>Returns the target object type.</returns>
        public static T GetActualObjectForSerializedProperty<T>(
            this SerializedProperty property, FieldInfo field, ref string label)
        {
            try
            {
                if (property == null || field == null)
                    return default;

                SerializedObject serializedObject = property.serializedObject;
                if (serializedObject == null)
                    return default;

                Object targetObject = serializedObject.targetObject;

                if (property.depth > 0)
                {
                    List<string> slicedName = property.propertyPath.Split('.').ToList();
                    List<int> arrayCounts = new List<int>();
                    for (int index = 0; index < slicedName.Count; index++)
                    {
                        arrayCounts.Add(-1);
                        string currName = slicedName[index];
                        if (currName.EndsWith("]"))
                        {
                            string[] arraySlice = currName.Split('[', ']');
                            if (arraySlice.Length >= 2)
                            {
                                arrayCounts[index - 2] = Convert.ToInt32(arraySlice[1]);
                                slicedName[index] = string.Empty;
                                slicedName[index - 1] = string.Empty;
                            }
                        }
                    }

                    while (string.IsNullOrEmpty(slicedName.Last()))
                    {
                        int i = slicedName.Count - 1;
                        slicedName.RemoveAt(i);
                        arrayCounts.RemoveAt(i);
                    }

                    if (property.propertyPath.EndsWith("]"))
                    {
                        string[] slice = property.propertyPath.Split('[', ']');
                        if (slice.Length >= 2)
                            label = "Element " + slice[slice.Length - 2];
                    }

                    return DescendHierarchy<T>(targetObject, slicedName, arrayCounts, 0);
                }

                object obj = field.GetValue(targetObject);
                return (T)obj;
            }
            catch
            {
                return default;
            }
        }

        static T DescendHierarchy<T>(object targetObject, List<string> splitName, List<int> splitCounts, int depth)
        {
            if (depth >= splitName.Count)
                return default;

            string currName = splitName[depth];

            if (string.IsNullOrEmpty(currName))
                return DescendHierarchy<T>(targetObject, splitName, splitCounts, depth + 1);

            int arrayIndex = splitCounts[depth];

            FieldInfo newField = targetObject.GetType().GetField(
                currName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (newField == null)
            {
                Type baseType = targetObject.GetType().BaseType;
                while (baseType != null && newField == null)
                {
                    newField = baseType.GetField(
                        currName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    baseType = baseType.BaseType;
                }
            }

            object newObj = newField.GetValue(targetObject);
            if (depth == splitName.Count - 1)
            {
                T actualObject = default(T);
                if (arrayIndex >= 0)
                {
                    if (newObj.GetType().IsArray && ((Array)newObj).Length > arrayIndex)
                        actualObject = (T)((Array)newObj).GetValue(arrayIndex);

                    if (newObj is IList newObjList && newObjList.Count > arrayIndex)
                    {
                        actualObject = (T)newObjList[arrayIndex];

                        //if (actualObject == null)
                        //    actualObject = new T();
                    }
                }
                else
                {
                    actualObject = (T)newObj;
                }

                return actualObject;
            }
            else if (arrayIndex >= 0)
            {
                if (newObj is IList list)
                    newObj = list[arrayIndex];
                else if (newObj is Array a)
                    newObj = a.GetValue(arrayIndex);
            }

            return DescendHierarchy<T>(newObj, splitName, splitCounts, depth + 1);
        }
        
        /// <summary>
        /// From: https://gist.github.com/monry/9de7009689cbc5050c652bcaaaa11daa
        /// </summary>
        public static SerializedProperty GetParent(this SerializedProperty serializedProperty)
        {
            string[] propertyPaths = serializedProperty.propertyPath.Split('.');
            if (propertyPaths.Length <= 1)
                return default;

            SerializedProperty parentSerializedProperty =
                serializedProperty.serializedObject.FindProperty(propertyPaths.First());
            for (int index = 1; index < propertyPaths.Length - 1; index++)
            {
                if (propertyPaths[index] == "Array")
                {
                    // Reached the end
                    if (index + 1 == propertyPaths.Length - 1)
                        break;
                    
                    if (propertyPaths.Length > index + 1 && Regex.IsMatch(propertyPaths[index + 1], "^data\\[\\d+\\]$"))
                    {
                        Match match = Regex.Match(propertyPaths[index + 1], "^data\\[(\\d+)\\]$");
                        int arrayIndex = int.Parse(match.Groups[1].Value);
                        parentSerializedProperty = parentSerializedProperty.GetArrayElementAtIndex(arrayIndex);
                        index++;
                    }
                    
                    continue;
                }

                parentSerializedProperty = parentSerializedProperty.FindPropertyRelative(propertyPaths[index]);
            }

            return parentSerializedProperty;
        }
        
        public static bool IsInArray(this SerializedProperty serializedProperty)
        {
            SerializedProperty parent = serializedProperty.GetParent();
            if (parent == null)
                return false;
            return parent.isArray;
        }
    }
}
