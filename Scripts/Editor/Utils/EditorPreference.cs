using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#if !UNITY_EDITOR
using System;
#endif // UNITY_EDITOR

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Utility class taken from Scaffolding to simplify the workflow of using EditorPrefs-serialized fields.
    ///
    /// Now you can specify a field that is to be serialized in EditorPrefs as follows:
    ///
    /// private EditorPreferenceBool generateIndirectAccess =
    ///     new EditorPreferenceBool("ScriptableObjectCollections/GenerateIndirectAccess");
    ///
    /// then you can get or set its value as follows:
    /// generateIndirectAccess.Value
    ///
    /// You can also draw the default editor field associated with it like so:
    /// generateIndirectAccess.DrawGUILayout();
    /// </summary>
    public abstract class EditorPreference
    {
        protected const string EditorOnlyExceptionMessage = "Can only be called in the Editor.";
        
        private string path;
        public string Path => path;

        private GUIContent label;
        protected GUIContent Label => label;

        public abstract object ObjectValue { get; }

        protected EditorPreference(string path)
        {
            this.path = path;

            string name = System.IO.Path.GetFileName(path).ToHumanReadable();
            label = new GUIContent(name);
        }

        public abstract void DrawGUILayout();
    }
        
    public abstract class EditorPreferenceGeneric<ValueType>
        : EditorPreference
    {
        public virtual ValueType Value
        {
            get => ValueRaw;
            set => ValueRaw = value;
        }

        protected ValueType ValueRaw
        {
            get
            {
#if !UNITY_EDITOR
                return ValueRuntime;
#else
                return !UnityEditor.EditorPrefs.HasKey(Path) ? defaultValue : UnityPrefsValue;
#endif // UNITY_EDITOR
            }
            set
            {
#if !UNITY_EDITOR
                // No need to set anything because we can't access the unity prefs anyway.
#else
                UnityPrefsValue = value;
#endif // UNITY_EDITOR
            }
        }

        protected virtual ValueType ValueRuntime => defaultValue;

        public override object ObjectValue => Value;

        protected abstract ValueType UnityPrefsValue { get; set; }

        private ValueType defaultValue;

        protected EditorPreferenceGeneric(string path, ValueType defaultValue = default(ValueType)) : base(path)
        {
            this.defaultValue = defaultValue;
        }
    }
        
    public class EditorPreferenceBool : EditorPreferenceGeneric<bool>
    {
        protected override bool UnityPrefsValue
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetBool(Path);
#else
                throw new Exception(EditorOnlyExceptionMessage);
#endif // !UNITY_EDITOR
            }
            set
            {
#if UNITY_EDITOR
                UnityEditor.EditorPrefs.SetBool(Path, value);
#else
                throw new Exception(EditorOnlyExceptionMessage);
#endif // !UNITY_EDITOR
            }
        }

        public EditorPreferenceBool(string path, bool defaultValue = default(bool)) : base(path, defaultValue)
        {
        }

        public override void DrawGUILayout()
        {
#if UNITY_EDITOR
            Value = UnityEditor.EditorGUILayout.Toggle(Label, Value);
#endif // UNITY_EDITOR
        }
    }
    
    public class EditorPreferenceString : EditorPreferenceGeneric<string>
    {
        protected override string UnityPrefsValue
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetString(Path);
#else
                throw new Exception(EditorOnlyExceptionMessage);
#endif // !UNITY_EDITOR
            }
            set
            {
#if UNITY_EDITOR
                UnityEditor.EditorPrefs.SetString(Path, value);
#else
                throw new Exception(EditorOnlyExceptionMessage);
#endif // !UNITY_EDITOR
            }
        }

        public EditorPreferenceString(string path, string defaultValue = default(string)) : base(path, defaultValue)
        {
        }

        public override void DrawGUILayout()
        {
#if UNITY_EDITOR
            Value = UnityEditor.EditorGUILayout.TextField(Label, Value);
#endif // UNITY_EDITOR
        }
    }
    
    public class EditorPreferenceInt : EditorPreferenceGeneric<int>
    {
        protected override int UnityPrefsValue
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetInt(Path);
#else
                throw new Exception(EditorOnlyExceptionMessage);
#endif // !UNITY_EDITOR
            }
            set
            {
#if UNITY_EDITOR
                UnityEditor.EditorPrefs.SetInt(Path, value);
#else
                throw new Exception(EditorOnlyExceptionMessage);
#endif // !UNITY_EDITOR
            }
        }

        public EditorPreferenceInt(string path, int defaultValue = default(int)) : base(path, defaultValue)
        {
        }

        public override void DrawGUILayout()
        {
#if UNITY_EDITOR
            Value = UnityEditor.EditorGUILayout.IntField(Label, Value);
#endif // UNITY_EDITOR
        }
    }
    
    public class EditorPreferenceFloat : EditorPreferenceGeneric<float>
    {
        protected override float UnityPrefsValue
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetFloat(Path);
#else
                throw new Exception(EditorOnlyExceptionMessage);
#endif // !UNITY_EDITOR
            }
            set
            {
#if UNITY_EDITOR
                UnityEditor.EditorPrefs.SetFloat(Path, value);
#else
                throw new Exception(EditorOnlyExceptionMessage);
#endif // !UNITY_EDITOR
            }
        }

        public EditorPreferenceFloat(string path, float defaultValue = default(float)) : base(path, defaultValue)
        {
        }

        public override void DrawGUILayout()
        {
#if UNITY_EDITOR
            Value = UnityEditor.EditorGUILayout.FloatField(Label, Value);
#endif // UNITY_EDITOR
        }
    }
    
    public class EditorPreferenceObject<T> : EditorPreferenceGeneric<T>
        where T : Object
    {
        private T cachedAsset;
        
        protected override T UnityPrefsValue
        {
            get
            {
#if UNITY_EDITOR
                if (cachedAsset == null)
                {
                    string assetPath = UnityEditor.EditorPrefs.GetString(Path);
                    
                    if (string.IsNullOrEmpty(assetPath))
                        return null;
                    
                    cachedAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
                }
                
                return cachedAsset;
#else
                throw new Exception(EditorOnlyExceptionMessage);
#endif // !UNITY_EDITOR
            }
            set
            {
#if UNITY_EDITOR
                T previousValue = UnityPrefsValue;
                if (previousValue != value)
                {
                    // Store the new asset's path.
                    string path = value == null ? null : UnityEditor.AssetDatabase.GetAssetPath(value);
                    UnityEditor.EditorPrefs.SetString(Path, path);
                    
                    // Invalidate the cached asset.
                    cachedAsset = null;
                }
#else
                throw new Exception(EditorOnlyExceptionMessage);
#endif // !UNITY_EDITOR
            }
        }

        public EditorPreferenceObject(string path, T defaultValue = default(T)) : base(path, defaultValue)
        {
        }

        public override void DrawGUILayout()
        {
#if UNITY_EDITOR
            Value = (T)UnityEditor.EditorGUILayout.ObjectField(Label, Value, typeof(T), false);
#endif // UNITY_EDITOR
        }
    }
}
