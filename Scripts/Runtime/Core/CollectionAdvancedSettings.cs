using System;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections 
{
    [Serializable]
    public struct CollectionAdvancedSettings 
    {
        [field: SerializeField]
        public bool AutomaticallyLoaded { get; private set; }

#if UNITY_EDITOR
        [field: SerializeField] 
        public bool GenerateAsPartialClass { get; private set; }

        [field: SerializeField] 
        public bool GenerateAsBaseClass { get; private set; }

        [field: SerializeField] 
        public string GeneratedFileLocationPath { get; private set; }

        [field: SerializeField] 
        public string GeneratedStaticClassFileName { get; private set; }

        [field: SerializeField] 
        public string GenerateStaticFileNamespace { get; private set; }
#endif

        public void SetDefaultValues() 
        {
            AutomaticallyLoaded = true;
#if UNITY_EDITOR
            GenerateAsPartialClass = true;
            GenerateAsBaseClass = default;
            GeneratedFileLocationPath = default;
            GeneratedStaticClassFileName = default;
            GenerateStaticFileNamespace = default;
#endif
        }
    }
}