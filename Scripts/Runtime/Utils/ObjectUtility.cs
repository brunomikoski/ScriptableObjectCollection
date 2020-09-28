namespace UnityEngine
{
    public static class ObjectUtility
    {
        public static void SetDirty(Object targetObject)
        {
            if (targetObject == null)
                return;
            if (Application.isPlaying)
                return;
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(targetObject);
#endif
        }
    }
}
