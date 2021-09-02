using System.Linq;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Core
{
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
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
            string registryGUID = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(TInstance).Name}")
                .FirstOrDefault();

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
