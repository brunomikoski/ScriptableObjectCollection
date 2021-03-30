using System.Collections.Generic;
using UnityEditor;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class EditorsCache
    {
        private static readonly Dictionary<Object, Editor> ItemToEditor = new Dictionary<Object, Editor>();
        
        public static Editor GetOrCreateEditorForItem(Object collectionItem)
        {
            if (ItemToEditor.TryGetValue(collectionItem, out Editor customEditor))
                return customEditor;
            
            Editor.CreateCachedEditor(collectionItem, null, ref customEditor);
            ItemToEditor.Add(collectionItem, customEditor);
            
            return customEditor;
        }
    }
}
