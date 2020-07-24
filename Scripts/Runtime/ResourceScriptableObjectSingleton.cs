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
                LoadOrCreateInstance();
                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void LoadOrCreateInstance()
        {
            if (instance != null)
                return;
            
            instance = FindObjectOfType<T>();

            if (instance != null)
                return;
            
            instance = Resources.Load<T>(typeof(T).Name);

            if (instance != null)
                return;

#if UNITY_EDITOR
            if (Application.isPlaying)
                return;
            
            string registryGUID = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(registryGUID))
            {
                instance = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(registryGUID));
            }
            
            if(instance != null)
                return;
            
            
            instance = CreateInstance<T>();

            AssetDatabaseUtils.CreatePathIfDontExist("Assets/Resources");
            UnityEditor.AssetDatabase.CreateAsset(instance, $"Assets/Resources/{typeof(T).Name}.asset");
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
    }
}
