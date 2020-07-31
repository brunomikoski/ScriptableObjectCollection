using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class CodeGenerationUtility
    {
        public static bool CreateNewEmptyScript(string fileName, string parentFolder, string nameSpace, string createAssetMenuInput, string classDeclarationString, params string[] directives)
        {
            AssetDatabaseUtils.CreatePathIfDontExist(parentFolder);
            string finalFilePath = Path.Combine(parentFolder, $"{fileName}.cs");

            if (File.Exists(Path.GetFullPath(finalFilePath)))
                return false;
            
            using (StreamWriter writer = new StreamWriter(finalFilePath))
            {
                bool hasNameSpace = !string.IsNullOrEmpty(nameSpace);
                int indentation = 0;

                foreach (string directive in directives)
                {
                    writer.WriteLine($"using {directive};");
                }
                
                writer.WriteLine();
                if (hasNameSpace)
                {
                    writer.WriteLine($"namespace {nameSpace}");
                    writer.WriteLine("{");
                    indentation++;
                }

                if (!string.IsNullOrEmpty(createAssetMenuInput))
                    writer.WriteLine($"{GetIndentation(indentation)}{createAssetMenuInput}");
                
                writer.WriteLine($"{GetIndentation(indentation)}{classDeclarationString}");
                writer.WriteLine(GetIndentation(indentation)+"{");
                indentation++;
                
                indentation--;
                writer.WriteLine(GetIndentation(indentation)+"}");
                
                if (hasNameSpace)
                    writer.WriteLine("}");
            }

            return true;
        }
        
        private static string GetIndentation(int identation)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < identation; i++)
            {
                stringBuilder.Append("    ");
            }

            return stringBuilder.ToString();
        }

        public static void AppendHeader(StreamWriter writer, ref int identation, string nameSpace, string className,
            bool isPartial, bool isStatic, params string[] directives)
        {
            writer.WriteLine("//  Automatically generated");
            writer.WriteLine("//");
            writer.WriteLine();
            for (int i = 0; i < directives.Length; i++)
            {
                string directive = directives[i];

                if (string.IsNullOrEmpty(directive))
                    continue;
                
                writer.WriteLine($"using {directive};");
            }

            writer.WriteLine();

            bool hasNameSpace = !string.IsNullOrEmpty(nameSpace);
            if (hasNameSpace)
            {
                writer.WriteLine($"namespace {nameSpace}");
                writer.WriteLine("{");

                identation++;
            }

            string finalClassDeclaration = "";
            finalClassDeclaration += GetIndentation(identation);
            finalClassDeclaration += "public ";
            if (isStatic)
                finalClassDeclaration += "static ";

            if (isPartial)
                finalClassDeclaration += "partial ";

            finalClassDeclaration += "class ";
            finalClassDeclaration += className;
            
            writer.WriteLine(finalClassDeclaration);
            writer.WriteLine(GetIndentation(identation) + "{");

            identation++;
        }

        public static void AppendLine(StreamWriter writer, int identation, string input = "")
        {
            writer.WriteLine($"{GetIndentation(identation)}{input}");
        }

        public static void AppendFooter(StreamWriter writer, ref int identation, string nameSpace)
        {
            bool hasNameSpace = !string.IsNullOrEmpty(nameSpace);
            if (hasNameSpace)
            {
                writer.WriteLine($"{GetIndentation(identation)}" + "}");
                identation--;
                writer.WriteLine($"{GetIndentation(identation)}" + "}");
            }
            else
            {
                identation--;
                writer.WriteLine($"{GetIndentation(identation)}" + "}");
            }
        }

        public static void GenerateStaticCollectionScript(ScriptableObjectCollection collection)
        {
            string dehumanizeCollectionName = collection.name.Sanitize();

            string fileName = $"{dehumanizeCollectionName.FirstToUpper()}Static";
            string nameSpace = collection.GetCollectionType().Namespace;
            string finalFolder = ScriptableObjectCollectionSettings.Instance.GetStaticFileFolderForCollection(collection);
            
            AssetDatabaseUtils.CreatePathIfDontExist(finalFolder);
            using (StreamWriter writer = new StreamWriter(Path.Combine(finalFolder, $"{fileName}.cs")))
            {
                int indentation = 0;
                
                List<string> directives = new List<string>();
                directives.Add(typeof(CollectionsRegistry).Namespace);
                directives.Add(collection.GetType().Namespace);
                directives.AddRange(GetCollectionDirectives(collection));

                AppendHeader(writer, ref indentation, nameSpace,
                    collection.GetType().ToString(), true, false, directives.Distinct().ToArray());

                GeneratedStaticFileType staticFileTypeForCollection = ScriptableObjectCollectionSettings.Instance.GetStaticFileTypeForCollection(collection);
                if (staticFileTypeForCollection == GeneratedStaticFileType.DirectAccess)
                    WriteDirectAccessCollectionStatic(collection, writer, ref indentation);
                else
                    WriteTryGetAccessCollectionStatic(collection, writer, ref indentation);

                indentation--;
                AppendFooter(writer, ref indentation, nameSpace);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static string[] GetCollectionDirectives(ScriptableObjectCollection collection)
        {
            HashSet<string> directives = new HashSet<string>();
            for (int i = 0; i < collection.Count; i++)
                directives.Add(collection[i].GetType().Namespace);

            return directives.ToArray();
        }

        private static void WriteTryGetAccessCollectionStatic(ScriptableObjectCollection collection, StreamWriter writer,
            ref int indentation)
        {
            string cachedValuesName = $"cached{collection.name.Sanitize().FirstToUpper()}Values";
            string valuesName = $"{collection.name.Sanitize().FirstToUpper()}Values";
            string tryGetValuesName = $"TryGet{valuesName}";

            AppendLine(writer, indentation, $"private static {collection.GetType().Name} {cachedValuesName};");

            for (int i = 0; i < collection.Items.Count; i++)
            {
                CollectableScriptableObject collectionItem = collection.Items[i];
                AppendLine(writer, indentation,
                    $"private static {collectionItem.GetType().Name} {collectionItem.name.Sanitize().FirstToLower()};");
            }

            AppendLine(writer, indentation);
            
            AppendLine(writer, indentation, $"public static bool {tryGetValuesName}(out {collection.GetType().Name} result)");
            AppendLine(writer, indentation, "{");
            indentation++;
            AppendLine(writer, indentation, $"if ({cachedValuesName} != null)");
            AppendLine(writer, indentation, "{");
            indentation++;
            AppendLine(writer, indentation, $"result = {cachedValuesName};");
            AppendLine(writer, indentation, "return true;");
            indentation--;
            AppendLine(writer, indentation, "}");
            AppendLine(writer, indentation);
            AppendLine(writer, indentation, $"if (!CollectionsRegistry.Instance.TryGetCollectionByGUID(\"{collection.GUID}\", out ScriptableObjectCollection resultCollection))");
            AppendLine(writer, indentation, "{");
            indentation++;
            AppendLine(writer, indentation, $"result = {cachedValuesName};");
            AppendLine(writer, indentation, "return false;");
            indentation--;
            AppendLine(writer, indentation, "}");
            AppendLine(writer, indentation);
            AppendLine(writer, indentation, $"{cachedValuesName} = ({collection.GetType().Name}) resultCollection;");
            AppendLine(writer, indentation, $"result = {cachedValuesName};");
            AppendLine(writer, indentation, $"return true;");
            indentation--;
            AppendLine(writer, indentation, "}");

            AppendLine(writer, indentation);

            for (int i = 0; i < collection.Items.Count; i++)
            {
                CollectableScriptableObject collectionItem = collection.Items[i];
                string pascalizedItemName = collectionItem.name.Sanitize().FirstToUpper();
                string camelizedItemName = collectionItem.name.Sanitize().FirstToLower();
                Type itemType = collectionItem.GetType();
                
                
                AppendLine(writer, indentation, $"public static bool TryGet{pascalizedItemName}(out {itemType.Name} result)");
                AppendLine(writer, indentation, "{");
                indentation++;
                AppendLine(writer, indentation, $"if ({camelizedItemName} != null)");
                AppendLine(writer, indentation, "{");
                indentation++;
                AppendLine(writer, indentation, $"result = {camelizedItemName};");
                AppendLine(writer, indentation, "return true;");
                indentation--;
                AppendLine(writer, indentation, "}");
                AppendLine(writer, indentation);
                
                AppendLine(writer, indentation, $"if (!{tryGetValuesName}(out {collection.GetType().Name} collectionResult))");
                AppendLine(writer, indentation, "{");
                indentation++;
                AppendLine(writer, indentation, "result = null;");
                AppendLine(writer, indentation, "return false;");
                indentation--;
                AppendLine(writer, indentation, "}");
                AppendLine(writer, indentation);

                
                AppendLine(writer, indentation, $"if (!collectionResult.TryGetCollectableByGUID(\"{collectionItem.GUID}\", out CollectableScriptableObject resultCollectable))");
                AppendLine(writer, indentation, "{");
                indentation++;
                AppendLine(writer, indentation, "result = null;");
                AppendLine(writer, indentation, "return false;");
                indentation--;
                AppendLine(writer, indentation);
                AppendLine(writer, indentation, "}");

                
                AppendLine(writer, indentation, $"{camelizedItemName} = ({itemType.Name}) resultCollectable;");
                AppendLine(writer, indentation, $"result = {camelizedItemName};");
                AppendLine(writer, indentation, "return true;");
                indentation--;
                AppendLine(writer, indentation, "}");
                AppendLine(writer, indentation);

            }
        }
        
        private static void WriteDirectAccessCollectionStatic(ScriptableObjectCollection collection, StreamWriter writer,
            ref int indentation)
        {
            string cachedValuesName = $"cached{collection.name.Sanitize().FirstToUpper()}Values";
            AppendLine(writer, indentation, $"private static {collection.GetType().Name} {cachedValuesName};");

            for (int i = 0; i < collection.Items.Count; i++)
            {
                CollectableScriptableObject collectionItem = collection.Items[i];
                AppendLine(writer, indentation,
                    $"private static {collectionItem.GetType().Name} {collectionItem.name.Sanitize().FirstToLower()};");
            }

            AppendLine(writer, indentation);

            string valuesName = $"{collection.name.Sanitize().FirstToUpper()}Values";
            AppendLine(writer, indentation,
                $"public static {collection.GetType().Name} {valuesName}");
            AppendLine(writer, indentation, "{");
            indentation++;
            AppendLine(writer, indentation, "get");
            AppendLine(writer, indentation, "{");
            indentation++;
            AppendLine(writer, indentation, $"if ({cachedValuesName} == null)");
            indentation++;
            AppendLine(writer, indentation,
                $"{cachedValuesName} = ({collection.GetType()})CollectionsRegistry.Instance.GetCollectionByGUID(\"{collection.GUID}\");");
            indentation--;
            AppendLine(writer, indentation, $"return {cachedValuesName};");
            indentation--;
            AppendLine(writer, indentation, "}");
            indentation--;
            AppendLine(writer, indentation, "}");
            AppendLine(writer, indentation);

            AppendLine(writer, indentation);

            for (int i = 0; i < collection.Items.Count; i++)
            {
                CollectableScriptableObject collectionItem = collection.Items[i];
                string collectionNameFirstUpper = collectionItem.name.Sanitize().FirstToUpper();
                string privateStaticName = collectionItem.name.Sanitize().FirstToLower();

                AppendLine(writer, indentation,
                    $"public static {collectionItem.GetType().Name} {collectionNameFirstUpper}");
                AppendLine(writer, indentation, "{");
                indentation++;
                AppendLine(writer, indentation, "get");
                AppendLine(writer, indentation, "{");
                indentation++;

                AppendLine(writer, indentation, $"if ({privateStaticName} == null)");
                indentation++;
                AppendLine(writer, indentation,
                    $"{privateStaticName} = ({collectionItem.GetType().Name}){valuesName}.GetCollectableByGUID(\"{collectionItem.GUID}\");");
                indentation--;
                AppendLine(writer, indentation, $"return {privateStaticName};");
                indentation--;
                AppendLine(writer, indentation, "}");
                indentation--;
                AppendLine(writer, indentation, "}");
                AppendLine(writer, indentation);
            }
        }
    }
}
