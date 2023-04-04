using System;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public class DelegateUtility
    {
        public static Delegate CastToType(Delegate source, Type targetType)
        {
            if (source == null) 
                return null;

            Delegate[] sourceDelegates = source.GetInvocationList();

            if (sourceDelegates.Length == 1)
                return Delegate.CreateDelegate(targetType, sourceDelegates[0].Target, sourceDelegates[0].Method);

            Delegate[] targetDelegates = new Delegate[sourceDelegates.Length];
            for (int i = 0; i < sourceDelegates.Length; i++)
                targetDelegates[i] = Delegate.CreateDelegate(targetType, sourceDelegates[i].Target, sourceDelegates[i].Method);

            return Delegate.Combine(targetDelegates);
        }
    }
}
