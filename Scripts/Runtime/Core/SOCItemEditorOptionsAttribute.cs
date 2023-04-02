using System;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public enum DrawType
    {
        Dropdown = 0,
        AsReference = 1
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class)]
    public class SOCItemEditorOptionsAttribute : Attribute
    {
        public DrawType DrawType { get; set; } = DrawType.Dropdown;

        public bool ShouldDrawGotoButton { get; set; } = true;

        public bool ShouldDrawPreviewButton { get; set; } = true;

        public string ValidateMethod { get; set; }
    }
}
