using UnityEditor;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class CopyCollectableUtils
    {
        private static ScriptableObjectCollectionItem source;

        public static void SetSource(ScriptableObjectCollectionItem targetSource)
        {
            source = targetSource;
        }

        public static bool CanPasteToTarget(ScriptableObjectCollectionItem target)
        {
            if (source == null)
                return false;

            return target.GetType() == source.GetType();
        }
        
        public static void ApplySourceToStart(ScriptableObjectCollectionItem target)
        {
            if (source == null)
                return;

            Undo.RecordObject(target, "Paste Changes");
            EditorUtility.CopySerializedManagedFieldsOnly(source, target);
            EditorUtility.SetDirty(target);
        }
        
    }
}
