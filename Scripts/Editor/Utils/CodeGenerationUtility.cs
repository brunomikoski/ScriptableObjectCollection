using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class CodeGenerationUtility
    {
        public static bool CreateNewEmptyScript(string fileName, string parentFolder, string nameSpace, string createAssetMenuInput, string classDeclarationString, params string[] directives)
        {
            AssetDatabaseUtils.CreatePathIfDontExist(parentFolder);
            string finalFilePath = Path.Combine(parentFolder, $"{fileName}.cs");

            if (File.Exists(PathUtils.RelativeToAbsolutePath(finalFilePath)))
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
    
        public static void AppendHeader(StreamWriter writer, ref int identation, string nameSpace, string filename,string className, bool isPartial, bool isStatic, 
            params string[] directives)
        {
            writer.WriteLine("//  Automatically generated");
            writer.WriteLine("//");
            writer.WriteLine();
            for (int i = 0; i < directives.Length; i++)
                writer.WriteLine($"using {directives[i]};");

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
            string collectionScriptName = collection.GetCollectionType().Name.Sanitize();
            string dehumanizeCollectionName = collection.name.Sanitize();

            string filename = $"{dehumanizeCollectionName.FirstToUpper()}Static";
            string nameSpace = collection.GetCollectionType().Namespace;
            string finalFolder = CollectionUtility.StaticGeneratedScriptsFolderPath;
            
            string assemblyForStaticContent = CompilationPipeline.GetAssemblyNameFromScriptPath(finalFolder);
            MonoScript scriptableObjectScript = MonoScript.FromScriptableObject(collection);
            string assemblyForScriptFromScriptableObject =
                CompilationPipeline.GetAssemblyNameFromScriptPath(AssetDatabase.GetAssetPath(scriptableObjectScript));

            if (!string.Equals(assemblyForStaticContent, assemblyForScriptFromScriptableObject,
                StringComparison.Ordinal))
            {
                Debug.LogError(
                    $"Cannot create file at path {PathUtils.FixPathForPlatform(Path.Combine(finalFolder, $"{filename}.cs"))}" +
                    $" since would be in a different assembly, Collection Assembly {assemblyForScriptFromScriptableObject} Static File Assembly: {assemblyForStaticContent}");
                return;
            }
            
            AssetDatabaseUtils.CreatePathIfDontExist(finalFolder);
            using (StreamWriter writer = new StreamWriter(Path.Combine(finalFolder, $"{filename}.cs")))
            {
                int indentation = 0;
                AppendHeader(writer, ref indentation, nameSpace, filename,
                    dehumanizeCollectionName.FirstToUpper(), true, false, typeof(CollectionsRegistry).Namespace);

                if (collection.StaticFileGenerationType == StaticFileType.DirectAccess)
                    WriteDirectAccessCollectionStatic(collection, writer, ref indentation);
                else
                    WriteTryGetAccessCollectionStatic(collection, writer, ref indentation);

                indentation--;
                AppendFooter(writer, ref indentation, nameSpace);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

         private static void WriteTryGetAccessCollectionStatic(ScriptableObjectCollection collection, StreamWriter writer,
            ref int indentation)
        {
            AppendLine(writer, indentation, $"private static {collection.GetType()} values;");

            for (int i = 0; i < collection.Items.Count; i++)
            {
                CollectableScriptableObject collectionItem = collection.Items[i];
                AppendLine(writer, indentation,
                    $"private static {collectionItem.GetType()} {collectionItem.name.Sanitize().FirstToLower()};");
            }

            AppendLine(writer, indentation);
            
            AppendLine(writer, indentation, $"public static bool TryGetValues(out {collection.GetType()} result)");
            AppendLine(writer, indentation, "{");
            indentation++;
            AppendLine(writer, indentation, "if (values != null)");
            AppendLine(writer, indentation, "{");
            indentation++;
            AppendLine(writer, indentation, "result = values;");
            AppendLine(writer, indentation, "return true;");
            indentation--;
            AppendLine(writer, indentation, "}");
            AppendLine(writer, indentation);
            AppendLine(writer, indentation, $"if (!CollectionsRegistry.Instance.TryGetCollectionByGUID(\"{collection.GUID}\", out ScriptableObjectCollection resultCollection))");
            AppendLine(writer, indentation, "{");
            indentation++;
            AppendLine(writer, indentation, "result = values;");
            AppendLine(writer, indentation, "return false;");
            indentation--;
            AppendLine(writer, indentation, "}");
            AppendLine(writer, indentation);
            AppendLine(writer, indentation, $"values = ({collection.GetType()}) resultCollection;");
            AppendLine(writer, indentation, $"result = values;");
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
                
                
                AppendLine(writer, indentation, $"public static bool TryGet{pascalizedItemName}(out {itemType} result)");
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
                
                AppendLine(writer, indentation, $"if (!TryGetValues(out {collection.GetType()} collectionResult))");
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

                
                AppendLine(writer, indentation, $"{camelizedItemName} = ({itemType}) resultCollectable;");
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
            AppendLine(writer, indentation, $"private static {collection.GetType()} values;");

            for (int i = 0; i < collection.Items.Count; i++)
            {
                CollectableScriptableObject collectionItem = collection.Items[i];
                AppendLine(writer, indentation,
                    $"private static {collectionItem.GetType()} {collectionItem.name.Sanitize().FirstToLower()};");
            }

            AppendLine(writer, indentation);

            AppendLine(writer, indentation,
                $"public static {collection.GetType()} Values");
            AppendLine(writer, indentation, "{");
            indentation++;
            AppendLine(writer, indentation, "get");
            AppendLine(writer, indentation, "{");
            indentation++;
            AppendLine(writer, indentation, "if (values == null)");
            indentation++;
            AppendLine(writer, indentation,
                $"values = ({collection.GetType()})CollectionsRegistry.Instance.GetCollectionByGUID(\"{collection.GUID}\");");
            indentation--;
            AppendLine(writer, indentation, "return values;");
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
                    $"public static {collectionItem.GetType()} {collectionNameFirstUpper}");
                AppendLine(writer, indentation, "{");
                indentation++;
                AppendLine(writer, indentation, "get");
                AppendLine(writer, indentation, "{");
                indentation++;

                AppendLine(writer, indentation, $"if ({privateStaticName} == null)");
                indentation++;
                AppendLine(writer, indentation,
                    $"{privateStaticName} = ({collectionItem.GetType()})Values.GetCollectableByGUID(\"{collectionItem.GUID}\");");
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
