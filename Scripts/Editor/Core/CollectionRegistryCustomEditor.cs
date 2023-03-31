using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [CustomEditor(typeof(CollectionsRegistry), true)]
    public sealed class CollectionRegistryCustomEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            if (GUILayout.Button("Find All Collections"))
            {
                CollectionsRegistry.Instance.ReloadCollections();
            }
            if (GUILayout.Button("Validate Collections"))
            {
                CollectionsRegistry.Instance.ValidateCollections();
            }
        }

        private void OnEnable()
        {
            CollectionsRegistry.Instance.ReloadCollections();
        }
    }
}
