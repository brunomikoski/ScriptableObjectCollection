namespace UnityEngine
{
    public static class ObjectUtility
    {
        public static void SetDirty(Object targetObject)
        {
#if UNITY_EDITOR
            if (targetObject == null)
                return;

            UnityEditor.EditorUtility.SetDirty(targetObject);
#endif
        }
    }
}
