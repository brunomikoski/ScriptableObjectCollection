using System;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public enum DrawType
    {
        Dropdown = 0,
        AsReference = 1
    }
    
    public enum LabelMode
    {
        Default = 0,
        NoLabel = 1,
        //LabelOnSeparateLine = 2, // TODO
    }

    public enum PreviewMode
    {
        /// <summary>
        /// Defer to the project-wide default set in Project Settings &gt; Scriptable Object Collection.
        /// </summary>
        Default = 0,
        /// <summary>
        /// Draw the item's properties as an inline panel beneath the field.
        /// </summary>
        Inline = 1,
        /// <summary>
        /// Open Unity's floating Property Editor window for the item. Useful when third-party
        /// inspectors (e.g. Odin) don't render correctly in the inline drawer.
        /// </summary>
        PropertyEditorWindow = 2,
    }
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class)]
    public class SOCItemEditorOptionsAttribute : Attribute
    {
        public DrawType DrawType { get; set; } = DrawType.Dropdown;

        public bool ShouldDrawGotoButton { get; set; } = true;

        public bool ShouldDrawPreviewButton { get; set; } = true;

        /// <summary>
        /// Controls how the preview button reveals the item. <see cref="PreviewMode.Default"/>
        /// falls back to the project-wide setting in <c>SOCSettings</c>.
        /// </summary>
        public PreviewMode PreviewMode { get; set; } = PreviewMode.Default;

        public string ValidateMethod { get; set; }
        
        /// <summary>
        /// If specified, only show collection items that belong to the collection assigned to the specified field.
        /// </summary>
        public string ConstrainToCollectionField { get; set; }

        public LabelMode LabelMode { get; set; }

        /// <summary>
        /// If specified, will perform this method whenever the value changes.
        /// Parameters of the method should be: ItemType from, ItemType to
        /// </summary>
        public string OnSelectCallbackMethod { get; set; }
    }
}
