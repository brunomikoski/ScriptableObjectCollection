using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// This method checks if the Unity Object is null. It casts it first to a generic
        /// C# object, to prevent the operator== of UnityEngine.Object to be called, which
        /// also check whether the C++ backend of the UnityEngine.Object is destroyed or not.
        ///
        /// See also: http://blogs.unity3d.com/2014/05/16/custom-operator-should-we-keep-it/
        /// </summary>
        public static bool IsNull(this Object o)
        {
            return ((object)o) == null;
        }

        public static bool IsNotNull(this Object o)
        {
            return ((object)o) != null;
        }
    }
}
