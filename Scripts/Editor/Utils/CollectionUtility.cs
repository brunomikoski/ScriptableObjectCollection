using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class CollectionUtility
    {
        private static Dictionary<int, bool> objectToFoldOut = new Dictionary<int, bool>();

        private static int GetHasCount(Object[] objects)
        {
            int hashValue = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                Object targetObj = objects[i];

                if (targetObj == null)
                    continue;

                hashValue += HashCode.Combine(hashValue, targetObj.GetHashCode());
            }

            return hashValue;
        }

        public static bool IsFoldoutOpen(params Object[] objects)
        {
            int hashCount = GetHasCount(objects);
           
            if (hashCount == 0)
                return false;

            if(!objectToFoldOut.TryGetValue(hashCount, out bool value))
                objectToFoldOut.Add(hashCount, value);

            return value;
        }

        public static void SetFoldoutOpen(bool value, params Object[] objects)
        {
            int hashCount = GetHasCount(objects);

            objectToFoldOut[hashCount] = value;
        }
    }
}

