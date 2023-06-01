using UnityEditor;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [InitializeOnLoad]
    public static class EditorBehaviour
    {
        static EditorBehaviour()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            if (playModeStateChange == PlayModeStateChange.EnteredPlayMode)
            {                
                CollectionsRegistry.Instance.RemoveNonAutomaticallyInitializedCollections();
            }
            else if (playModeStateChange == PlayModeStateChange.EnteredEditMode)
            {
                if (CollectionsRegistry.Instance.AutoSearchForCollections)
                    CollectionsRegistry.Instance.ReloadCollections();
            }
        }
    }
}
