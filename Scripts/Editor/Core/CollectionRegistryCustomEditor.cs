using UnityEditor;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomEditor(typeof(CollectionsRegistry), true)]
    public sealed class CollectionRegistryCustomEditor : Editor
    {
        private void OnEnable()
        {
            CollectionsRegistry.Instance.ReloadCollections();
        }
    }
}
