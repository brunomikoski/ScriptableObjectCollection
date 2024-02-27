using System;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public enum DrawType
    {
        Dropdown = 0,
        AsReference = 1
    }
    
#if UNITY_2022_2_OR_NEWER
    [Obsolete("DrawAsSOCItemAttribute is not needed anymore, since Unity 2022 PropertyDrawers can be applied to interfaces")]
#endif
    [AttributeUsage(AttributeTargets.Field)]
    public class DrawAsSOCItemAttribute : PropertyAttribute
    {
        
    }
    
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class)]
    public class SOCItemEditorOptionsAttribute : Attribute
    {
        public DrawType DrawType { get; set; } = DrawType.Dropdown;

        public bool ShouldDrawGotoButton { get; set; } = true;

        public bool ShouldDrawPreviewButton { get; set; } = true;

        public string ValidateMethod { get; set; }
        
        /// <summary>
        /// If specified, only show collection items that belong to the collection assigned to the specified field.
        /// </summary>
        public string ConstrainToCollectionField { get; set; }
    }

    //Temporary 
    [Obsolete("CollectionItemEditorOptions is deprecated, please use SOCItemEditorOptionsAttribute instead.")]
    public class CollectionItemEditorOptions : SOCItemEditorOptionsAttribute
    {
    }
}
