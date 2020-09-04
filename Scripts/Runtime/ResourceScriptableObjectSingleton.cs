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
        public static T LoadOrCreateInstance<T>() where T: ScriptableObject
        {
            T newInstance = Resources.Load<T>(typeof(T).Name);

            if (newInstance != null)
                return newInstance;

#if UNITY_EDITOR
            if (Application.isPlaying)
                return null;
            
            string registryGUID = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(registryGUID))
            {
                newInstance = (T) UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(registryGUID));
            }

            if (newInstance != null)
                return newInstance;
            
            newInstance = CreateInstance<T>();

            AssetDatabaseUtils.CreatePathIfDontExist("Assets/Resources");
            UnityEditor.AssetDatabase.CreateAsset(newInstance, $"Assets/Resources/{typeof(T).Name}.asset");
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            return newInstance;
#endif
#pragma warning disable CS0162
            return null;
#pragma warning restore CS0162
        }
    }
}
