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
        public string Path
        {
            get
            {
                if (IsProjectSpecific)
                    return ProjectPrefix + path;
                
                return path;
            }
        }

        private bool isProjectSpecific;
        public bool IsProjectSpecific
        {
            get => isProjectSpecific;
            set => isProjectSpecific = value;
        }

        private static string cachedProjectPrefix;

        private static string ProjectPrefix
        {
            get
            {
                if (cachedProjectPrefix == null)
                {
                    string assetsFolder = Application.dataPath;
                    string projectsFolder = assetsFolder.Substring(0, assetsFolder.Length - "/Assets".Length);
                    
                    // NOTE: If you only want the name of the project checkout, you can do that here. That would
                    // conflate save data from checkouts in different drives/parent folders with the same name though,
                    // so I'm choosing to include the path up until the project name too.
                    //string projectName = System.IO.Path.GetFileName(projectsFolder);
                 
                    // Prefix ends up something like C:/Git/YourProjectName/ so it's unique to your checkout.
                    cachedProjectPrefix = projectsFolder + "/";
                }
                return cachedProjectPrefix;
            }
        }

        private GUIContent label;
        public GUIContent Label => label;

        public abstract object ObjectValue { get; }

        protected EditorPreference(string path, bool isProjectSpecific = false)
        {
            this.path = path;
            this.isProjectSpecific = isProjectSpecific;

            string name = System.IO.Path.GetFileName(path).ToHumanReadable();
            label = new GUIContent(name);
        }

        public abstract void DrawGUILayout(GUIContent label, params GUILayoutOption[] options);

        public void DrawGUILayout(params GUILayoutOption[] layoutOptions)
        {
            DrawGUILayout(Label, layoutOptions);
        }
        
        public void DrawGUILayout(string label, params GUILayoutOption[] options)
        {
            DrawGUILayout(new GUIContent(label), options);
        }
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

        protected EditorPreferenceGeneric(
            string path, ValueType defaultValue = default(ValueType), bool isProjectSpecific = false)
            : base(path, isProjectSpecific)
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

        public EditorPreferenceBool(string path, bool defaultValue = default(bool), bool isProjectSpecific = false)
            : base(path, defaultValue, isProjectSpecific)
        {
        }

        public override void DrawGUILayout(GUIContent label, params GUILayoutOption[] options)
        {
#if UNITY_EDITOR
            Value = UnityEditor.EditorGUILayout.Toggle(label, Value, options);
#endif // UNITY_EDITOR
        }

        public void DrawGUILayoutLeft(params GUILayoutOption[] options)
        {
            DrawGUILayoutLeft(Label, options);
        }

        public void DrawGUILayoutLeft(GUIContent label, params GUILayoutOption[] options)
        {
#if UNITY_EDITOR
            Value = UnityEditor.EditorGUILayout.ToggleLeft(label, Value, options);
#endif // UNITY_EDITOR
        }
        
        public void DrawGUILayoutLeft(string label, params GUILayoutOption[] options)
        {
            DrawGUILayoutLeft(new GUIContent(label), options);
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

        public EditorPreferenceString(string path, string defaultValue = default(string), bool isProjectSpecific = false)
            : base(path, defaultValue, isProjectSpecific)
        {
        }

        public override void DrawGUILayout(GUIContent label, params GUILayoutOption[] options)
        {
#if UNITY_EDITOR
            Value = UnityEditor.EditorGUILayout.TextField(label, Value, options);
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

        public EditorPreferenceInt(string path, int defaultValue = default(int), bool isProjectSpecific = false)
            : base(path, defaultValue, isProjectSpecific)
        {
        }

        public override void DrawGUILayout(GUIContent label, params GUILayoutOption[] options)
        {
#if UNITY_EDITOR
            Value = UnityEditor.EditorGUILayout.IntField(label, Value, options);
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

        public EditorPreferenceFloat(string path, float defaultValue = default(float), bool isProjectSpecific = false)
            : base(path, defaultValue)
        {
        }

        public override void DrawGUILayout(GUIContent label, params GUILayoutOption[] options)
        {
#if UNITY_EDITOR
            Value = UnityEditor.EditorGUILayout.FloatField(label, Value, options);
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

        public EditorPreferenceObject(string path, T defaultValue = default(T), bool isProjectSpecific = false)
            : base(path, defaultValue, isProjectSpecific)
        {
        }

        public override void DrawGUILayout(GUIContent label, params GUILayoutOption[] options)
        {
#if UNITY_EDITOR
            Value = (T)UnityEditor.EditorGUILayout.ObjectField(label, Value, typeof(T), false, options);
#endif // UNITY_EDITOR
        }
    }
}
