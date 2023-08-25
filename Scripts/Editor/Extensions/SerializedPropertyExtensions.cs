using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

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
        
        public static void SetValue(this SerializedProperty serializedProperty, object value)
        {
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
                case SerializedPropertyType.Generic:
                default:
                    Debug.LogWarning(
                        $"Tried to copy value '{value}' from a template to an SOC item but apparently that's not supported.");
                    break;
            }
        }
    }
}
