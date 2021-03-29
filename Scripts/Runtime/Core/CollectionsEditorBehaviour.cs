#if UNITY_EDITOR
using UnityEditor;

namespace BrunoMikoski.ScriptableObjectCollections
{
    [InitializeOnLoad]
    public static class CollectionsEditorBehaviour 
    {
        static CollectionsEditorBehaviour()
        {
            EditorApplication.playModeStateChanged  -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged  += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange playModeState)
        {
            if (CollectionsRegistry.Instance == null)
                return;
            
            if (playModeState == PlayModeStateChange.EnteredPlayMode)
                CollectionsRegistry.Instance.PrepareForPlayMode();
            else if(playModeState == PlayModeStateChange.ExitingPlayMode)
                CollectionsRegistry.Instance.PrepareForEditorMode();
        }
    }
}
#endif
