using System;
using System.Collections.Generic;
using System.Linq;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class TypeUtility
    {
        private static List<Type> cachedAvailableTypes;

        private static List<Type> AvailableTypes => cachedAvailableTypes ?? (cachedAvailableTypes = GetTypesFromAssemblies());

        private static List<Type> GetTypesFromAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => x != null)
                .SelectMany(x => x.GetTypes())
                .ToList();
        }

        private static Dictionary<Type, List<Type>> typeToSubclasses = new Dictionary<Type, List<Type>>();
        
        public static List<Type> GetAllSubclasses(Type targetType, bool ignoreCache = false)
        {
            if (ignoreCache || !typeToSubclasses.TryGetValue(targetType, out List<Type> results))
            {
                results = AvailableTypes.Where(t => t.IsClass && t.IsSubclassOf(targetType)).ToList();
                if(!ignoreCache)
                    typeToSubclasses.Add(targetType, results);
            }
            return results;
        }
    }
}
