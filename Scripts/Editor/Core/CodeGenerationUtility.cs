using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class CodeGenerationUtility
    {
        public static bool CreateNewScript(
            string fileName, string parentFolder, string nameSpace, string[] directives, params string[] lines)
        {
            // Make sure the folder exists.
            AssetDatabaseUtils.CreatePathIfDoesntExist(parentFolder);
            
            // Check that the file doesn't exist yet.
            string finalFilePath = Path.Combine(parentFolder, $"{fileName}.cs");
            if (File.Exists(Path.GetFullPath(finalFilePath)))
                return false;

            using StreamWriter writer = new StreamWriter(finalFilePath);
            int indentation = 0;

            // First write the directives.
            if (directives != null && directives.Length > 0)
            {
                foreach (string directive in directives)
                {
                    if (string.IsNullOrWhiteSpace(directive))
                        continue;

                    writer.WriteLine($"using {directive};");
                }
                writer.WriteLine();
            }
            
            // Then write the namespace.
            bool hasNameSpace = !string.IsNullOrEmpty(nameSpace);
            if (hasNameSpace)
            {
                writer.WriteLine($"namespace {nameSpace}");
                writer.WriteLine("{");
                indentation++;
            }

            // Add the contents of the file.
            if (lines != null)
            {
                foreach (string line in lines)
                {
                    if (line == "}")
                        indentation--;
                    
                    writer.WriteLine(GetIndentation(indentation) + line);
                    
                    if (line == "{")
                        indentation++;
                }
            }

            // If necessary, end the namespace.
            if (hasNameSpace)
                writer.WriteLine("}");

            return true;
        }

        public static bool CreateNewScript(string fileName, string parentFolder, string nameSpace,
            string classAttributes, string classDeclarationString, string[] innerContent, params string[] directives)
        {
            List<string> lines = new List<string>();
            int indentation = 0;
            
            // Add class definition
            if (!string.IsNullOrEmpty(classAttributes))
                lines.Add($"{GetIndentation(indentation)}{classAttributes}");
            lines.Add($"{GetIndentation(indentation)}{classDeclarationString}");
            
            // Start class braces
            lines.Add(GetIndentation(indentation)+"{");
            indentation++;
            
            // Add class inner content
            if (innerContent != null)
            {
                foreach (string content in innerContent)
                {
                    if (content == "}")
                        indentation--;
                    
                    lines.Add(GetIndentation(indentation)+content);
                    
                    if (content == "{")
                        indentation++;
                }
            }
            
            // End class braces
            indentation--;
            lines.Add(GetIndentation(indentation)+"}");

            return CreateNewScript(fileName, parentFolder, nameSpace, directives, lines.ToArray());
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

        public static void DisablePartialClassGenerationIfDisallowed(ScriptableObjectCollection collection)
        {
            SerializedObject collectionSerializedObject = new SerializedObject(collection);

            bool canBePartial = CheckIfCanBePartial(collection);
            SerializedProperty partialClassSP = collectionSerializedObject.FindProperty("generateAsPartialClass");
            if (partialClassSP.boolValue && !canBePartial)
            {
                partialClassSP.boolValue = false;
                collectionSerializedObject.ApplyModifiedProperties();
            }
        }
        public static bool CheckIfCanBePartial(ScriptableObjectCollection collection)
        {
            SerializedObject collectionSerializedObject = new SerializedObject(collection);

            string baseClassPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(collection));
            string baseAssembly = CompilationPipeline.GetAssemblyNameFromScriptPath(baseClassPath);
            string targetGeneratedCodePath = CompilationPipeline.GetAssemblyNameFromScriptPath(collectionSerializedObject.FindProperty("generatedFileLocationPath").stringValue);
            
            // NOTE: If you're not using assemblies for your code, it's expected that 'targetGeneratedCodePath' would
            // be the same as 'baseAssembly', but it isn't. 'targetGeneratedCodePath' seems to be empty in that case.
            bool canBePartial = baseAssembly.Equals(targetGeneratedCodePath, StringComparison.Ordinal) ||
                                string.IsNullOrEmpty(targetGeneratedCodePath);
            
            return canBePartial;
        }

        public static void GenerateStaticCollectionScript(ScriptableObjectCollection collection)
        {
            if (!CanGenerateStaticFile(collection, out string errorMessage))
            {
                Debug.LogError(errorMessage);
                return;
            }

            DisablePartialClassGenerationIfDisallowed(collection);

            SerializedObject collectionSerializedObject = new SerializedObject(collection);
            string fileName = collectionSerializedObject.FindProperty("generatedStaticClassFileName").stringValue;
            
            string nameSpace = collectionSerializedObject.FindProperty("generateStaticFileNamespace").stringValue;
            
            string finalFolder = collectionSerializedObject.FindProperty("generatedFileLocationPath").stringValue;
            
            bool writeAsPartial = collectionSerializedObject.FindProperty("generateAsPartialClass").boolValue;
            bool useBaseClass = collectionSerializedObject.FindProperty("generateAsBaseClass").boolValue;


            AssetDatabaseUtils.CreatePathIfDoesntExist(finalFolder);
            using (StreamWriter writer = new StreamWriter(Path.Combine(finalFolder, $"{fileName}.cs")))
            {
                int indentation = 0;
                
                List<string> directives = new List<string>();
                directives.Add(typeof(CollectionsRegistry).Namespace);
                directives.Add(collection.GetType().Namespace);
                directives.Add(typeof(List<>).Namespace);
                directives.Add("System");
                directives.AddRange(GetCollectionDirectives(collection));
                string className = collection.GetItemType().Name;

                if (!writeAsPartial)
                    className = fileName;
                
                else if (className.Equals(nameof(ScriptableObject)))
                {
                    Debug.LogWarning($"Cannot create static class using the collection type name ({nameof(ScriptableObject)})"+
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
            CollectionsRegistry.Instance.ValidateCollections();
            
            List<ScriptableObjectCollection> collectionsOfSameType = CollectionsRegistry.Instance.GetCollectionsByItemType(collection.GetItemType());
            if (collectionsOfSameType.Count > 1)
            {
                for (int i = 0; i < collectionsOfSameType.Count; i++)
                {
                    ScriptableObjectCollection collectionA = collectionsOfSameType[i];
                    SerializedObject collectionASO = new SerializedObject(collectionA);


                    string targetNamespaceA = collectionASO.FindProperty("generateStaticFileNamespace").stringValue;
                    string targetFileNameA = collectionASO.FindProperty("generatedStaticClassFileName").stringValue;

                    for (int j = 0; j < collectionsOfSameType.Count; j++)
                    {
                        if (i == j)
                            continue;

                        ScriptableObjectCollection collectionB = collectionsOfSameType[j];
                        SerializedObject collectionBSO = new SerializedObject(collectionB);

                        
                        string targetNamespaceB = collectionBSO.FindProperty("generateStaticFileNamespace").stringValue;
                        string targetFileNameB = collectionBSO.FindProperty("generatedStaticClassFileName").stringValue;

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
            string hasCachedValuesNames = "hasCachedValues";
            AppendLine(writer, indentation, $"private static bool {hasCachedValuesNames};");
            AppendLine(writer, indentation, $"private static {collection.GetType().Name} {cachedValuesName};");

            AppendLine(writer, indentation);

            for (int i = 0; i < collection.Items.Count; i++)
            {
                ScriptableObject collectionItem = collection.Items[i];
                Type type = useBaseClass ? collection.GetItemType() : collectionItem.GetType();
                AppendLine(writer, indentation, 
                    $"private static bool hasCached{collectionItem.name.Sanitize().FirstToUpper()};");
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
            AppendLine(writer, indentation, $"if (!{hasCachedValuesNames})");
            indentation++;
            (long, long) collectionGUIDValues = collection.GUID.GetRawValues();
            AppendLine(writer, indentation,
                $"{hasCachedValuesNames} = CollectionsRegistry.Instance.TryGetCollectionByGUID(new LongGuid({collectionGUIDValues.Item1}, {collectionGUIDValues.Item2}), out {cachedValuesName});");
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
                string privateStaticCachedName = $"cached{collectionNameFirstUpper}";
                string privateHasCachedName = $"hasCached{collectionNameFirstUpper}";
                Type type = useBaseClass ? collection.GetItemType() : collectionItem.GetType();

                ISOCItem socItem = collectionItem as ISOCItem;
                if (socItem == null)
                    continue;
                
                AppendLine(writer, indentation, $"public static {type.FullName} {collectionNameFirstUpper}");
                AppendLine(writer, indentation, "{");
                indentation++;
                AppendLine(writer, indentation, "get");
                AppendLine(writer, indentation, "{");
                indentation++;

                AppendLine(writer, indentation, $"if (!{privateHasCachedName})");
                indentation++;
                (long, long) collectionItemGUIDValues = socItem.GUID.GetRawValues();
                AppendLine(writer, indentation,
                    $"{privateHasCachedName} = Values.TryGetItemByGUID(new LongGuid({collectionItemGUIDValues.Item1}, {collectionItemGUIDValues.Item2}), out {privateStaticCachedName});");
                indentation--;
                AppendLine(writer, indentation, $"return {privateStaticCachedName};");
                indentation--;
                AppendLine(writer, indentation, "}");
                indentation--;
                AppendLine(writer, indentation, "}");
                AppendLine(writer, indentation);
            }
            
            AppendLine(writer, indentation);
        }

        public static bool DoesStaticFileForCollectionExist(ScriptableObjectCollection collection)
        {
            SerializedObject collectionSerializedObject = new SerializedObject(collection);
            string fileName = collectionSerializedObject.FindProperty("generatedStaticClassFileName").stringValue;
            string finalFolder = collectionSerializedObject.FindProperty("generatedFileLocationPath").stringValue;
            return File.Exists(Path.Combine(finalFolder, $"{fileName}.cs"));
        }
    }
}
