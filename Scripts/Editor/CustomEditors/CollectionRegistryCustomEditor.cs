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
            
            if (GUILayout.Button("Reload Collections"))
                CollectionsRegistry.Instance.ReloadCollections();

            if (GUILayout.Button("Validate Collections"))
                CollectionsRegistry.Instance.ValidateCollections();

            if (GUILayout.Button("Generate All Existent Static Access Files"))
                GenerateAllExistentStaticAccessFiles();

            CollectionsRegistry.Instance.SetAutoSearchForCollections(GUILayout.Toggle(CollectionsRegistry.Instance.AutoSearchForCollections, "Auto Search For Collections"));
        }

        private void GenerateAllExistentStaticAccessFiles()
        {
            for (int i = 0; i < CollectionsRegistry.Instance.Collections.Count; i++)
            {
                ScriptableObjectCollection collection = CollectionsRegistry.Instance.Collections[i];
                if (!CodeGenerationUtility.DoesStaticFileForCollectionExist(collection))
                    continue;

                CodeGenerationUtility.GenerateStaticCollectionScript(collection);
            }
        }

        private void OnEnable()
        {
            CollectionsRegistry.Instance.ReloadCollections();
        }
    }
}
