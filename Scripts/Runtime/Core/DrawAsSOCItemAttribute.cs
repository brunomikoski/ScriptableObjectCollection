using System;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
#if UNITY_2022_2_OR_NEWER
    [Obsolete("DrawAsSOCItemAttribute is not needed anymore, since Unity 2022 PropertyDrawers can be applied to interfaces")]
#endif
    [AttributeUsage(AttributeTargets.Field)]
    public class DrawAsSOCItemAttribute : PropertyAttribute
    {
        
    }
}
