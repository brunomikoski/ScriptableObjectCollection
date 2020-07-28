using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class TypeUtility
    {
        private static List<Type> cachedAvaliableTypes;

        private static List<Type> AvailableTypes
        {
            get
            {
                if (cachedAvaliableTypes != null)
                    return cachedAvaliableTypes;
                
                cachedAvaliableTypes = new List<Type>();
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    Assembly assembly = assemblies[i];
                    if (assembly == null)
                        continue;
                    
                    cachedAvaliableTypes.AddRange(assembly.GetTypes());
                }

                return cachedAvaliableTypes;
            }
        }

        private static Dictionary<Type, List<Type>> typeToSubclasses = new Dictionary<Type, List<Type>>();
        
        public static List<Type> GetAllSubclasses(Type targetType, bool ignoreCache = false)
        {
            if (ignoreCache || !typeToSubclasses.TryGetValue(targetType, out List<Type> results))
            {
                results = AvailableTypes.Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(targetType)).ToList();
                if(!ignoreCache)
                    typeToSubclasses.Add(targetType, results);
            }
            return results;
        }
    }
}
