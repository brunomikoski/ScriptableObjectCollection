using UnityEditor;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class CopyCollectableUtils
    {
        private static CollectableScriptableObject source;

        public static void SetSource(CollectableScriptableObject targetSource)
        {
            source = targetSource;
        }

        public static bool CanPasteToTarget(CollectableScriptableObject target)
        {
            if (source == null)
                return false;

            return target.GetType() == source.GetType();
        }
        
        public static void ApplySourceToStart(CollectableScriptableObject target)
        {
            if (source == null)
                return;

            Undo.RecordObject(target, "Paste Changes");
            EditorUtility.CopySerializedManagedFieldsOnly(source, target);
            EditorUtility.SetDirty(target);
        }
        
    }
}
