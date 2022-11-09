using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections 
{
    [CreateAssetMenu(menuName = "ScriptableObject Collection/Settings/Create Advanced Settings", fileName = "Advanced Settings")]
    public class ScriptableObjectCollectionAdvancedSettings : ScriptableObject {

#pragma warning disable 0414
        [field: SerializeField] 
        public CollectionAdvancedSettings Settings { get; private set; }
#pragma warning restore 0414
    }
}