using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public sealed class CollectionItemDropdownItem : AdvancedDropdownItem
    {
        internal readonly ScriptableObject CollectionItem;

        public CollectionItemDropdownItem(ScriptableObject target) : base(target.name)
        {
            CollectionItem = target;
        }
    }
}
