using System.Collections.Generic;
using UnityEditor;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public class EditorsCache
    {
        private static Dictionary<Object, Editor> itemToEditor = new Dictionary<Object, Editor>();
        
        public static Editor GetOrCreateEditorForItem(Object collectionItem)
        {
            if (itemToEditor.TryGetValue(collectionItem, out Editor customEditor))
                return customEditor;
            
            Editor.CreateCachedEditor(collectionItem, null, ref customEditor);
            itemToEditor.Add(collectionItem, customEditor);
            
            return customEditor;
        }
    }
}
