using System;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public enum DrawType
    {
        Dropdown = 0,
        AsReference = 1
    }
    
    [AttributeUsage(AttributeTargets.Field)]
    public class CollectableEditorOptionsAttribute :Attribute
    {
        private DrawType drawType = DrawType.Dropdown;
        public DrawType DrawType
        {
            get => drawType;
            set => drawType = value;
        }

        public CollectableEditorOptionsAttribute(DrawType drawType)
        {
            this.drawType = drawType;
        }
    }
}
