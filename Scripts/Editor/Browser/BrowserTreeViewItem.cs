using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Browser
{
    public class BrowserTreeViewItem : TreeViewItem
    {
        public ScriptableObject ScriptableObject { get; private set; }

        public BrowserTreeViewItem(int id, int depth, ScriptableObject scriptableObject)
            : base(id, depth, scriptableObject.name)
        {
            ScriptableObject = scriptableObject;
        }
    }
}
