using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace BrunoMikoski.ScriptableObjectCollections
{
    [Serializable]
    public class ScriptableObjectReferenceItem
    {
        [SerializeField] 
        private string targetGuid;
        public string TargetGuid => targetGuid;

        public TObject Load<TObject>()
#if UNITY_EDITOR 
        where TObject : UnityEngine.Object
#endif
        {
            TObject asset = default;
            if (!string.IsNullOrEmpty(TargetGuid))
            {
#if UNITY_EDITOR
                asset = AssetDatabase.LoadAssetAtPath<TObject>(AssetDatabase.GUIDToAssetPath(targetGuid));
#else
                throw new InvalidOperationException("Addressables wasn't detected! Please install it in order to use ScriptableObjectReferenceItems.");
#endif
            }
            return asset;
        }
    }
}