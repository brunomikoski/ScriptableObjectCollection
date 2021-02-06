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
        public static T2 LoadOrCreateInstance<T2>() where T2 : ScriptableObject
        {
            if (!TryToLoadInstance(out T2 resultInstance))
            {
#if UNITY_EDITOR
                resultInstance = CreateInstance<T2>();

                AssetDatabaseUtils.CreatePathIfDontExist("Assets/Resources");
                UnityEditor.AssetDatabase.CreateAsset(resultInstance, $"Assets/Resources/{typeof(T2).Name}.asset");
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

        private static bool TryToLoadInstance<T3>(out T3 result) where T3 : ScriptableObject
        {
            T3 newInstance = Resources.Load<T3>(typeof(T3).Name);

            if (newInstance != null)
            {
                result = newInstance;
                return true;
            }

#if UNITY_EDITOR
            string registryGUID = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T3).Name}")
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(registryGUID))
            {
                newInstance = (T3) UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(
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
