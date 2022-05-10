using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if SOC_ADDRESSABLES
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace BrunoMikoski.ScriptableObjectCollections
{
    [Serializable]
    public class ScriptableObjectReferenceItem
    {
#if SOC_ADDRESSABLES
        [System.NonSerialized]
        public AsyncOperationHandle handle;
#endif
        [SerializeField] private string targetGuid;

        public string TargetGuid
        {
            get => targetGuid;
            set => targetGuid = value;
        }

        public TObject Load<TObject>()
#if UNITY_EDITOR && !SOC_ADDRESSABLES
        where TObject : UnityEngine.Object
#endif
        {
            TObject asset = default;
            if (!string.IsNullOrEmpty(TargetGuid))
            {
#if SOC_ADDRESSABLES
                try
                {
                    var typeHandle = Addressables.LoadAssetAsync<TObject>(TargetGuid);
                    asset = typeHandle.WaitForCompletion();
                    handle = typeHandle;
                }
                catch (Exception exception)
                {
                    Debug.LogError(exception);
                    throw;
                }

#elif UNITY_EDITOR
                asset = AssetDatabase.LoadAssetAtPath<TObject>(AssetDatabase.GUIDToAssetPath(targetGuid));
#else
                throw new Exception("Addressables wasn't detected! Please install it in order to use ScriptableObjectReferenceItems.")
#endif
            }
            return asset;
        }

#if SOC_ADDRESSABLES
        public async Task<TObject> LoadAsync<TObject>()
        {
            TObject asset = default;
            if (!string.IsNullOrEmpty(TargetGuid))
            {
                var typeHandle = 
                    Addressables.LoadAssetAsync<TObject>(TargetGuid);
                await typeHandle.Task;
                asset = typeHandle.Result;
                handle = typeHandle;
            }
            return asset;
        }

        public void Unload()
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }
#endif
    }
}