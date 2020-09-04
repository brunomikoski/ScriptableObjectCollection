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
            T2 newInstance = Resources.Load<T2>(typeof(T2).Name);

            if (newInstance != null)
                return newInstance;

#if UNITY_EDITOR
            if (Application.isPlaying)
                return null;
            
            string registryGUID = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T2).Name}")
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(registryGUID))
            {
                newInstance = (T2) UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(registryGUID));
            }

            if (newInstance != null)
                return newInstance ;
            
            newInstance = CreateInstance<T2>();

            AssetDatabaseUtils.CreatePathIfDontExist("Assets/Resources");
            UnityEditor.AssetDatabase.CreateAsset(newInstance, $"Assets/Resources/{typeof(T2).Name}.asset");
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
