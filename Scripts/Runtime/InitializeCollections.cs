using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{    
    [DefaultExecutionOrder(-10000)]
    public class InitializeCollections : MonoBehaviour
    {
        [SerializeField]
        private ScriptableObjectCollection[] collections;

        private void Awake()
        {
            for (int i = 0; i < collections.Length; i++)
            {
                CollectionsRegistry.Instance.RegisterCollection(collections[i]);
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < collections.Length; i++)
            {
                CollectionsRegistry.Instance.UnregisterCollection(collections[i]);
            }
        }
    }
}