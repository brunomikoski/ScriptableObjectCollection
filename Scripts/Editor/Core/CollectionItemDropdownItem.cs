using UnityEditor.IMGUI.Controls;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public sealed class CollectionItemDropdownItem : AdvancedDropdownItem
    {
        internal readonly ScriptableObjectCollectionItem CollectionItem;

        public CollectionItemDropdownItem(ScriptableObjectCollectionItem target) : base(target.name)
        {
            CollectionItem = target;
        }
    }
}
