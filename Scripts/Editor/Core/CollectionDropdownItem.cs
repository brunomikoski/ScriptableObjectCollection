using UnityEditor.IMGUI.Controls;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public sealed class CollectionDropdownItem : AdvancedDropdownItem
    {
        internal ScriptableObjectCollection collection;

        public CollectionDropdownItem(ScriptableObjectCollection scriptableObjectCollection) : base(scriptableObjectCollection.name)
        {
            collection = scriptableObjectCollection;
        }

        public CollectionDropdownItem(string displayValue) : base(displayValue)
        {
            collection = null;
        }
    }
}
