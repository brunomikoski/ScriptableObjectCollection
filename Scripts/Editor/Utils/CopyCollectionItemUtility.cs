using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class EditorCache
    {
        private static Dictionary<object, Editor> typeToEditorCache = new();

        public static Editor GetOrCreateEditorForObject(Object targetObject)
        {
            if (typeToEditorCache.TryGetValue(targetObject, out Editor editor))
                return editor;
            
            editor = Editor.CreateEditor(targetObject);
            typeToEditorCache.Add(targetObject, editor);
            return editor;
        }

    }
    public static class CopyCollectionItemUtility
    {
        private static ScriptableObject source;

        public static void SetSource(ScriptableObject targetSource)
        {
            source = targetSource;
        }

        public static bool CanPasteToTarget(ScriptableObject target)
        {
            if (source == null)
                return false;

            return target.GetType() == source.GetType();
        }
        
        public static void ApplySourceToTarget(ScriptableObject target)
        {
            if (source == null)
                return;

            Undo.RecordObject(target, "Paste Changes");
            EditorUtility.CopySerializedManagedFieldsOnly(source, target);
            EditorUtility.SetDirty(target);
        }
        
    }
}
