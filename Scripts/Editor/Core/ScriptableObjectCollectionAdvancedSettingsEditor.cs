using UnityEditor;

namespace BrunoMikoski.ScriptableObjectCollections 
{
    [CustomEditor(typeof(ScriptableObjectCollectionAdvancedSettings), true)]
    public sealed class ScriptableObjectCollectionAdvancedSettingsEditor : Editor 
    {
        // This editor exists solely to allow the usage of the auto layout system on the Advanced Settings
        // property drawer (prevents ArgumentException: Getting control 1's position in a group with only 1
        // controls when doing repaint).
    }
}
