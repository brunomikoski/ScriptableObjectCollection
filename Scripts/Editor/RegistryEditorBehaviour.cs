using UnityEditor;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [InitializeOnLoad]
    public class RegistryEditorBehaviour
    {
        static RegistryEditorBehaviour()
        {
         
            EditorApplication.playModeStateChanged += EditorApplicationOnplayModeStateChanged;
        }

        private static void EditorApplicationOnplayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            if (playModeStateChange == PlayModeStateChange.EnteredPlayMode)
            {
                CollectionsRegistry.Instance.RemoveNonAutomaticallyInitializedCollections();
            }
            else if (playModeStateChange == PlayModeStateChange.EnteredEditMode)
            {
                CollectionsRegistry.Instance.ReloadCollections();
            }
        }
    }
}