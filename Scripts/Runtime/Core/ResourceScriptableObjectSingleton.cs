using UnityEngine;
using UnityEngine.Scripting;

namespace BrunoMikoski.ScriptableObjectCollections.Core
{
    [Preserve]
    public class ResourceScriptableObjectSingleton<T> : ScriptableObject where T: ScriptableObject
    {
        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                    instance = LoadOrCreateInstance<T>();
                return instance;
            }
        }

        public static TInstance LoadOrCreateInstance<TInstance>() where TInstance : ScriptableObject
        {
            if (!TryToLoadInstance<TInstance>(out TInstance resultInstance))
            {
#if !UNITY_EDITOR
                return null;
#else
                resultInstance = CreateInstance<TInstance>();

                AssetDatabaseUtils.CreatePathIfDontExist("Assets/Resources");
                UnityEditor.AssetDatabase.CreateAsset(resultInstance, $"Assets/Resources/{typeof(TInstance).Name}.asset");
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
                return resultInstance;
#endif         
            }

            return resultInstance;
        }

        public static bool Exist()
        {
            return TryToLoadInstance<T>(out _);
        }

        private static bool TryToLoadInstance<TInstance>(out TInstance result) where TInstance: ScriptableObject
        {
            TInstance newInstance = Resources.Load<TInstance>(typeof(TInstance).Name);

            if (newInstance != null)
            {
                result = newInstance;
                return true;
            }

#if UNITY_EDITOR
            string[] assets = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(TInstance).Name}");
            
            string registryGUID = "";

            if (assets.Length > 0)
                registryGUID = assets[0];

            if (!string.IsNullOrEmpty(registryGUID))
            {
                newInstance = (TInstance) UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(registryGUID));
            }

            if (newInstance != null)
            {
                result = newInstance;
                return true;
            }
#endif
            result = null;
            return false;
        }
        
    }
}
