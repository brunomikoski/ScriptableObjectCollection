using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class CodeGenerationUtility
    {
        public static bool CreateNewEmptyScript(string fileName, string parentFolder, string nameSpace,
            string classAttributes, string classDeclarationString, string[] innerContent, params string[] directives)
        {
            AssetDatabaseUtils.CreatePathIfDoesntExist(parentFolder);
            string finalFilePath = Path.Combine(parentFolder, $"{fileName}.cs");

            if (File.Exists(Path.GetFullPath(finalFilePath)))
                return false;
            
            using (StreamWriter writer = new StreamWriter(finalFilePath))
            {
                bool hasNameSpace = !string.IsNullOrEmpty(nameSpace);
                int indentation = 0;

                foreach (string directive in directives)
                {
                    if (string.IsNullOrWhiteSpace(directive))
                        continue;
                    
                    writer.WriteLine($"using {directive};");
                }
                
                writer.WriteLine();
                if (hasNameSpace)
                {
                    writer.WriteLine($"namespace {nameSpace}");
                    writer.WriteLine("{");
                    indentation++;
                }

                if (!string.IsNullOrEmpty(classAttributes))
                    writer.WriteLine($"{GetIndentation(indentation)}{classAttributes}");
                
                writer.WriteLine($"{GetIndentation(indentation)}{classDeclarationString}");
                writer.WriteLine(GetIndentation(indentation)+"{");
                indentation++;
                if(innerContent != null)
                {
                    foreach (var content in innerContent)
                    {
                        if (content == "}") indentation--;
                        writer.WriteLine(GetIndentation(indentation)+content);
                        if (content == "{") indentation++;
                    }
                }
                indentation--;
                writer.WriteLine(GetIndentation(indentation)+"}");
                
                if (hasNameSpace)
                    writer.WriteLine("}");
            }

            return true;
        }
        
        private static string GetIndentation(int indentation)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < indentation; i++)
            {
                stringBuilder.Append("    ");
            }

            return stringBuilder.ToString();
        }

        private static void AppendHeader(StreamWriter writer, ref int indentation, string nameSpace, string classAttributes, string className,
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

                indentation++;
            }

            if (!string.IsNullOrEmpty(classAttributes))
                writer.WriteLine($"{GetIndentation(indentation)}{classAttributes}");
            
            string finalClassDeclaration = "";
            finalClassDeclaration += GetIndentation(indentation);
            finalClassDeclaration += "public ";
            if (isStatic)
                finalClassDeclaration += "static ";

            if (isPartial)
                finalClassDeclaration += "partial ";

            finalClassDeclaration += "class ";
            finalClassDeclaration += className;
            
            writer.WriteLine(finalClassDeclaration);
            writer.WriteLine(GetIndentation(indentation) + "{");

            indentation++;
        }
        
        
        public static void AppendHeader(StreamWriter writer, ref int indentation, string nameSpace, string classAttributes, string classDeclaration, params string[] directives)
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

                indentation++;
            }

            if (!string.IsNullOrEmpty(classAttributes))
                writer.WriteLine($"{GetIndentation(indentation)}{classAttributes}");

            writer.WriteLine($"{GetIndentation(indentation)}{classDeclaration}");
            writer.WriteLine(GetIndentation(indentation) + "{");

            indentation++;
        }
        

        public static void AppendLine(StreamWriter writer, int indentation, string input = "")
        {
            writer.WriteLine($"{GetIndentation(indentation)}{input}");
        }

        public static void AppendFooter(StreamWriter writer, ref int indentation, string nameSpace)
        {
            bool hasNameSpace = !string.IsNullOrEmpty(nameSpace);
            if (hasNameSpace)
            {
                writer.WriteLine($"{GetIndentation(indentation)}" + "}");
                indentation--;
                writer.WriteLine($"{GetIndentation(indentation)}" + "}");
            }
            else
            {
                indentation--;
                writer.WriteLine($"{GetIndentation(indentation)}" + "}");
            }
        }

        public static void GenerateStaticCollectionScript(ScriptableObjectCollection collection)
        {
            if (!CanGenerateStaticFile(collection, out string errorMessage))
            {
                Debug.LogError(errorMessage);
                return;
            }

            string fileName = CollectionsRegistry.Instance.CollectionSettings.GetCollectionGeneratedStaticClassFileName(collection);
            string nameSpace = CollectionsRegistry.Instance.CollectionSettings.GetCollectionGeneratedStaticFileNamespace(collection);
            string finalFolder = CollectionsRegistry.Instance.CollectionSettings.GetCollectionGeneratedFileLocationPath(collection);
            bool writeAsPartial = CollectionsRegistry.Instance.CollectionSettings.IsCollectionGenerateAsPartialClass(collection);
            bool useBaseClass = CollectionsRegistry.Instance.CollectionSettings.IsCollectionGenerateAsBaseClass(collection);


            AssetDatabaseUtils.CreatePathIfDoesntExist(finalFolder);
            using (StreamWriter writer = new StreamWriter(Path.Combine(finalFolder, $"{fileName}.cs")))
            {
                int indentation = 0;
                
                List<string> directives = new List<string>();
                directives.Add(typeof(CollectionsRegistry).Namespace);
                directives.Add(collection.GetType().Namespace);
                directives.Add(typeof(List<>).Namespace);
                directives.Add("System.Linq");
                directives.Add("System");
                directives.AddRange(GetCollectionDirectives(collection));
                string className = collection.GetItemType().Name;

                if (!writeAsPartial)
                {
                    className = fileName;
                }
                else if (className.Equals(nameof(ScriptableObjectCollectionItem)))
                {
                    Debug.LogWarning($"Cannot create static class using the collection type name ({nameof(ScriptableObjectCollectionItem)})"+
                        $"The \"Static File Name\" ({fileName}) will be used as its class name instead.");
                    className = fileName;
                }

                AppendHeader(writer, ref indentation, nameSpace, "", className,
                    writeAsPartial,
                    false, directives.Distinct().ToArray()
                );

                WriteDirectAccessCollectionStatic(collection, writer, ref indentation, useBaseClass);

                indentation--;
                AppendFooter(writer, ref indentation, nameSpace);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static bool CanGenerateStaticFile(ScriptableObjectCollection collection, out string errorMessage)
        {
            List<ScriptableObjectCollection> collectionsOfSameType = CollectionsRegistry.Instance.GetCollectionsByItemType(collection.GetItemType());
            if (collectionsOfSameType.Count > 1)
            {
                for (int i = 0; i < collectionsOfSameType.Count; i++)
                {
                    ScriptableObjectCollection collectionA = collectionsOfSameType[i];
                    string targetNamespaceA =
                        CollectionsRegistry.Instance.CollectionSettings.GetCollectionGeneratedStaticFileNamespace(collectionA);
                    string targetFileNameA =
                        CollectionsRegistry.Instance.CollectionSettings.GetCollectionGeneratedStaticClassFileName(collectionA);

                    for (int j = 0; j < collectionsOfSameType.Count; j++)
                    {
                        if (i == j)
                            continue;

                        ScriptableObjectCollection collectionB = collectionsOfSameType[j];

                        string targetNamespaceB =
                            CollectionsRegistry.Instance.CollectionSettings.GetCollectionGeneratedStaticFileNamespace(collectionB);

                        string targetFileNameB =
                            CollectionsRegistry.Instance.CollectionSettings.GetCollectionGeneratedStaticClassFileName(collectionB);

                        if (targetFileNameA.Equals(targetFileNameB, StringComparison.Ordinal)
                            && targetNamespaceA.Equals(targetNamespaceB, StringComparison.Ordinal))
                        {
                            errorMessage =
                                $"Two collections ({collectionA.name} and {collectionB.name}) with the same name and namespace already exist, please use custom ones";
                            return false;
                        }
                    }
                }
            }

            errorMessage = String.Empty;
            return true;
        }
       
        private static string[] GetCollectionDirectives(ScriptableObjectCollection collection)
        {
            HashSet<string> directives = new HashSet<string>();
            for (int i = 0; i < collection.Count; i++)
                directives.Add(collection[i].GetType().Namespace);

            return directives.ToArray();
        }
        
        private static void WriteDirectAccessCollectionStatic(ScriptableObjectCollection collection, StreamWriter writer,
            ref int indentation, bool useBaseClass)
        {
            string cachedValuesName = "values";
            AppendLine(writer, indentation, $"private static {collection.GetType().Name} {cachedValuesName};");

            AppendLine(writer, indentation);

            for (int i = 0; i < collection.Items.Count; i++)
            {
                ScriptableObject collectionItem = collection.Items[i];
                Type type = useBaseClass ? collection.GetItemType() : collectionItem.GetType();
                AppendLine(writer, indentation, 
                    $"private static {type.FullName} cached{collectionItem.name.Sanitize().FirstToUpper()};");
            }

            AppendLine(writer, indentation);

            string valuesName = $"Values";

            AppendLine(writer, indentation,
                $"public static {collection.GetType().FullName} {valuesName}");
            
            AppendLine(writer, indentation, "{");
            indentation++;
            AppendLine(writer, indentation, "get");
            AppendLine(writer, indentation, "{");
            indentation++;
            AppendLine(writer, indentation, $"if ({cachedValuesName} == null)");
            indentation++;
            (long, long) collectionGUIDValues = collection.GUID.GetValue();
            AppendLine(writer, indentation,
                $"{cachedValuesName} = ({collection.GetType()})CollectionsRegistry.Instance.GetCollectionByGUID(new LongGuid({collectionGUIDValues.Item1}, {collectionGUIDValues.Item2}));");
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
                ScriptableObject collectionItem = collection.Items[i];
                string collectionNameFirstUpper = collectionItem.name.Sanitize().FirstToUpper();
                string privateStaticName = $"cached{collectionNameFirstUpper}";
                Type type = useBaseClass ? collection.GetItemType() : collectionItem.GetType();

                ISOCItem socItem = collectionItem as ISOCItem;
                if (socItem == null)
                    continue;
                
                AppendLine(writer, indentation,
                    $"public static {type.FullName} {collectionNameFirstUpper}");
                AppendLine(writer, indentation, "{");
                indentation++;
                AppendLine(writer, indentation, "get");
                AppendLine(writer, indentation, "{");
                indentation++;

                AppendLine(writer, indentation, $"if ({privateStaticName} == null)");
                indentation++;
                (long, long) collectionItemGUIDValues = socItem.GUID.GetValue();
                AppendLine(writer, indentation,
                    $"{privateStaticName} = ({type.FullName}){valuesName}.GetItemByGUID(new LongGuid({collectionItemGUIDValues.Item1}, {collectionItemGUIDValues.Item2}));");
                indentation--;
                AppendLine(writer, indentation, $"return {privateStaticName};");
                indentation--;
                AppendLine(writer, indentation, "}");
                indentation--;
                AppendLine(writer, indentation, "}");
                AppendLine(writer, indentation);
            }
            
            
            AppendLine(writer, indentation, $"public static IEnumerable<T> GetValues<T>() where T : {collection.GetItemType().Name}");
            AppendLine(writer, indentation, "{");
            indentation++;
            AppendLine(writer, indentation, $"return Values.Where(item => item is T).Cast<T>();");
            indentation--;
            AppendLine(writer, indentation, "}");
            
            AppendLine(writer, indentation);
        }
    }
}
