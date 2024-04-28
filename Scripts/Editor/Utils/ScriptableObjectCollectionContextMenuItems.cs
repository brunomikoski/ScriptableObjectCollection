using System;
using UnityEditor;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class ScriptableObjectCollectionContextMenuItems
    {
        [MenuItem("CONTEXT/ScriptableObjectCollection/Create Generator", false, 99999)]
        private static void CreateGenerator(MenuCommand command)
        {
            Type collectionType = command.context.GetType();

            GeneratorCreationWizard.Show(collectionType);
        }

        [MenuItem("CONTEXT/ScriptableObjectCollection/Create Generator", true)]
        private static bool CreateGeneratorValidator(MenuCommand command)
        {
            Type collectionType = command.context.GetType();
            return CollectionGenerators.GetGeneratorTypeForCollection(collectionType) == null;
        }

        [MenuItem("CONTEXT/ScriptableObjectCollection/Edit Generator", false, 99999)]
        private static void EditGenerator(MenuCommand command)
        {
            Type collectionType = command.context.GetType();
            Type generatorType = CollectionGenerators.GetGeneratorTypeForCollection(collectionType);

            if (ScriptUtility.TryGetScriptOfClass(generatorType, out MonoScript script))
                AssetDatabase.OpenAsset(script);
        }

        [MenuItem("CONTEXT/ScriptableObjectCollection/Edit Generator", true)]
        private static bool EditGeneratorValidator(MenuCommand command)
        {
            Type collectionType = command.context.GetType();
            return CollectionGenerators.GetGeneratorTypeForCollection(collectionType) != null;
        }
        
        
        [MenuItem("CONTEXT/ScriptableObjectCollection/Create Indirect Reference file", false, 99999)]
        private static void CreateIndirectReference(MenuCommand command)
        {
            ScriptableObjectCollection collection = (ScriptableObjectCollection)command.context;
            
            CodeGenerationUtility.GenerateIndirectAccessForCollectionItemType(collection.GetItemType());
        }
    }
}