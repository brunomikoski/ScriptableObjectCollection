using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Browser
{
#if UNITY_6000_1_OR_NEWER
    public class BrowserTreeViewItem : TreeViewItem<int>
#else
    public class BrowserTreeViewItem : TreeViewItem
#endif
    {
        public ScriptableObject ScriptableObject { get; private set; }

        public BrowserTreeViewItem(int id, int depth, ScriptableObject scriptableObject)
            : base(id, depth, scriptableObject.name)
        {
            ScriptableObject = scriptableObject;
        }
    }
}
