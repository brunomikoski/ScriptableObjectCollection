using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;
#if ADDRESSABLES_ENABLED
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class CodeGenerationUtility
    {
        private const string PrivateValuesName = "cachedValues";
        private const string PublicValuesName = "Values";
        private const string HasCachedValuesName = "hasCachedValues";
        private const string ExtensionOld = ".cs";
        private const string ExtensionNew = ".g.cs";
        

        public static bool CreateNewScript(
            string fileName, string parentFolder, string nameSpace, string[] directives, params string[] lines)
        {
            parentFolder = parentFolder.ToPathWithConsistentSeparators();
            
            // Make sure the folder exists.
            AssetDatabaseUtils.CreatePathIfDoesntExist(parentFolder);
            
            // Check if the created folder is an editor folder.
            const string editorFolderName = "Editor";
            bool isEditorFolder = parentFolder.Contains($"/{editorFolderName}/") ||
                                  parentFolder.EndsWith($"/{editorFolderName}");
            if (isEditorFolder)
            {
                // Figure out what the last editor folder is. This is because you can create an item with path
                // 'Assets/ProjectName/Scripts/Editor/SomeSubFolder', in which case it should be making an asmref
                // for 'Assets/ProjectName/Scripts/Editor' and not for the subfolder.
                int lastOccurrenceOfEditorName = parentFolder.LastIndexOf($"/{editorFolderName}", 
                    StringComparison.OrdinalIgnoreCase);
                string editorFolderPath = parentFolder.Substring(
                    0, lastOccurrenceOfEditorName + editorFolderName.Length + 1);
                    
                // Find out if there is an editor asmdef that we should be referencing.
                AssemblyDefinitionAsset editorAsmDefToReference = AsmDefUtility
                    .GetParentEditorAsmDef(editorFolderPath);
                if (editorAsmDefToReference != null)
                {
                    // If so, add an asmref for it, otherwise it might not be able to reference editor code correctly.
                    AsmDefUtility.AddAsmRefToTopLevelEditorFolder(editorFolderPath);
                }
            }
            
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
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    
                    line = line.TrimStart();

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
            
            writer.Close();

            return true;
        }
        
        public static bool CreateNewScript(
            string fileName, string parentFolder, string nameSpace, string[] directives, 
            string codeTemplateFileName, Dictionary<string, string> replacements)
        {
            // Try to find the specified code template.
            string[] codeTemplateCandidates = AssetDatabase.FindAssets($"t:TextAsset {codeTemplateFileName}.cs");
            TextAsset codeTemplate = null;
            if (codeTemplateCandidates.Length > 0)
            {
                string codeTemplatePath = AssetDatabase.GUIDToAssetPath(codeTemplateCandidates[0]);
                codeTemplate = AssetDatabase.LoadAssetAtPath<TextAsset>(codeTemplatePath);
            }
            
            // Make sure the template exists.
            if (codeTemplateCandidates.Length == 0 || codeTemplate == null)
            {
                Debug.LogError($"Tried to create new script '{parentFolder}/{fileName}' but code template " +
                               $"'{codeTemplateFileName}.cs.txt' could not be found. Make sure this template exists.");
                return false;
            }
            
            string codeTemplateText = codeTemplate.text;
            
            // Apply any specified replacements.
            foreach (KeyValuePair<string,string> tagToReplacement in replacements)
            {
                codeTemplateText = codeTemplateText.Replace($"##{tagToReplacement.Key}##", tagToReplacement.Value);
            }
            
            // Now create the script.
            string[] lines = codeTemplateText.Split("\r\n");
            return CreateNewScript(fileName, parentFolder, nameSpace, directives, lines);
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
            bool canBePartial = CheckIfCanBePartial(collection);
            if (SOCSettings.Instance.GetWriteAsPartialClass(collection) && !canBePartial)
            {
                SOCSettings.Instance.SetWriteAsPartialClass(collection, false);
            }
        }
        public static bool CheckIfCanBePartial(ScriptableObjectCollection collection, string destinationFolder = "")
        {
            string baseClassPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(collection));
            string baseAssembly = CompilationPipeline.GetAssemblyNameFromScriptPath(baseClassPath);
            if (string.IsNullOrEmpty(destinationFolder))
            {
                destinationFolder = SOCSettings.Instance.GetParentFolderPathForCollection(collection);
            }

            string destinationFolderAssembly = CompilationPipeline.GetAssemblyNameFromScriptPath(destinationFolder);

            // NOTE: If you're not using assemblies for your code, it's expected that 'targetGeneratedCodePath' would
            // be the same as 'baseAssembly', but it isn't. 'targetGeneratedCodePath' seems to be empty in that case.
            bool canBePartial = baseAssembly.Equals(destinationFolderAssembly, StringComparison.Ordinal) ||
                                string.IsNullOrEmpty(destinationFolder);
            
            return canBePartial;
        }

        public static void GenerateIndirectAccessForCollectionItemType(Type collectionItemType)
        {
            string baseClassPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(ScriptableObject.CreateInstance(collectionItemType)));
            string parentFolder = Path.GetDirectoryName(baseClassPath);
            GenerateIndirectAccessForCollectionItemType(collectionItemType.Name, collectionItemType.Namespace, parentFolder);
        }

        public static void GenerateIndirectAccessForCollectionItemType(string collectionName, string collectionNamespace,
            string targetFolder)
        {
            string fileName = $"{collectionName}IndirectReference";

            AssetDatabaseUtils.CreatePathIfDoesntExist(targetFolder);
            
            string targetFileName = Path.Combine(targetFolder, fileName);
            
            // Delete any existing files that have the old deprecated extension.
            string deprecatedFileName = targetFileName + ExtensionOld;
            if (AssetDatabase.AssetPathExists(deprecatedFileName))
            {
                Debug.LogWarning($"Deleting deprecated Indirect Access file '{deprecatedFileName}'.");
                AssetDatabase.DeleteAsset(deprecatedFileName);
            }

            targetFileName += ExtensionNew;
            using (StreamWriter writer = new StreamWriter(targetFileName))
            {
                int indentation = 0;
                List<string> directives = new List<string>();
                directives.Add(typeof(ScriptableObjectCollection).Namespace);
                
                directives.Add(collectionNamespace);
                directives.Add("System");
                directives.Add("UnityEngine");

                AppendHeader(writer, ref indentation, collectionNamespace, "[Serializable]",
                    $"public sealed class {collectionName}IndirectReference : CollectionItemIndirectReference<{collectionName}>",
                    directives.Distinct().ToArray());

                AppendLine(writer, indentation,
                    $"public {collectionName}IndirectReference() {{}}");
                
                AppendLine(writer, indentation,
                    $"public {collectionName}IndirectReference({collectionName} collectionItemScriptableObject) : base(collectionItemScriptableObject) {{}}");

                indentation--;
                AppendFooter(writer, ref indentation, collectionNamespace);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void GenerateStaticCollectionScript(ScriptableObjectCollection collection)
        {
            if (!CanGenerateStaticFile(collection, out string errorMessage))
            {
                Debug.LogError(errorMessage);
                return;
            }

            DisablePartialClassGenerationIfDisallowed(collection);

            string fileName = SOCSettings.Instance.GetStaticFilenameForCollection(collection);
            string nameSpace = SOCSettings.Instance.GetNamespaceForCollection(collection);
            string finalFolder = AssetDatabase.GetAssetPath(SOCSettings.Instance.GetParentDefaultAssetScriptsFolderForCollection(collection));
            
            bool writeAsPartial = SOCSettings.Instance.GetWriteAsPartialClass(collection);
            bool useBaseClass = SOCSettings.Instance.GetUseBaseClassForItem(collection);


            AssetDatabaseUtils.CreatePathIfDoesntExist(finalFolder);

            string finalFileName = Path.Combine(finalFolder, fileName);
            
            // Delete any existing files that have the old deprecated extension.
            string deprecatedFileName = finalFileName + ExtensionOld;
            if (File.Exists(deprecatedFileName))
            {
                Debug.LogWarning($"Deleting deprecated Static Access file '{deprecatedFileName}'.");
                AssetDatabase.DeleteAsset(deprecatedFileName);
            }
            
            finalFileName += ExtensionNew;
            using (StreamWriter writer = new StreamWriter(finalFileName))
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

                if (!collection.AutomaticallyLoaded)
                {
                    WriteNonAutomaticallyLoadedCollectionItems(collection, writer, ref indentation, useBaseClass);
                }

                
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
                    
                    string targetNamespaceA = SOCSettings.Instance.GetNamespaceForCollection(collectionA);
                    string targetFileNameA = SOCSettings.Instance.GetStaticFilenameForCollection(collectionA);

                    for (int j = 0; j < collectionsOfSameType.Count; j++)
                    {
                        if (i == j)
                            continue;

                        ScriptableObjectCollection collectionB = collectionsOfSameType[j];
                        
                        string targetNamespaceB = SOCSettings.Instance.GetNamespaceForCollection(collectionB);
                        string targetFileNameB = SOCSettings.Instance.GetStaticFilenameForCollection(collectionB);

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

            if (!collection.AutomaticallyLoaded)
            {
#if ADDRESSABLES_ENABLED
                directives.Add("UnityEngine.AddressableAssets");
                directives.Add("UnityEngine.ResourceManagement.AsyncOperations");
#endif
            }

            return directives.ToArray();
        }
        
        private static void WriteDirectAccessCollectionStatic(ScriptableObjectCollection collection, StreamWriter writer,
            ref int indentation, bool useBaseClass)
        {
            AppendLine(writer, indentation, $"private static bool {HasCachedValuesName};");
            AppendLine(writer, indentation, $"private static {collection.GetType().Name} {PrivateValuesName};");

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


            AppendLine(writer, indentation,
                $"public static {collection.GetType().FullName} {PublicValuesName}");
            
            AppendLine(writer, indentation, "{");
            indentation++;
            AppendLine(writer, indentation, "get");
            AppendLine(writer, indentation, "{");
            indentation++;
            AppendLine(writer, indentation, $"if (!{HasCachedValuesName})");
            indentation++;
            (long, long) collectionGUIDValues = collection.GUID.GetRawValues();
            AppendLine(writer, indentation,
                $"{HasCachedValuesName} = CollectionsRegistry.Instance.TryGetCollectionByGUID(new LongGuid({collectionGUIDValues.Item1}, {collectionGUIDValues.Item2}), out {PrivateValuesName});");
            indentation--;
            AppendLine(writer, indentation, $"return {PrivateValuesName};");
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
                    $"{privateHasCachedName} = {PublicValuesName}.TryGetItemByGUID(new LongGuid({collectionItemGUIDValues.Item1}, {collectionItemGUIDValues.Item2}), out {privateStaticCachedName});");
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
        
        private static void WriteNonAutomaticallyLoadedCollectionItems(ScriptableObjectCollection collection, StreamWriter writer, ref int indentation, bool useBaseClass)
        {
            AppendLine(writer, indentation,
                $"public static bool IsCollectionLoaded()");
            
            AppendLine(writer, indentation, "{");
            indentation++;
            AppendLine(writer, indentation, $"return {PublicValuesName} != null;");
            indentation--;
            AppendLine(writer, indentation, "}");

            AppendLine(writer, indentation);


            if (!SOCSettings.Instance.GetWriteAddressableLoadingMethods(collection))
            {
                return;
            }
#if ADDRESSABLES_ENABLED
            string assetPath = AssetDatabase.GetAssetPath(collection);
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));
            
            if (entry != null)
            {
                AppendLine(writer, indentation, $"private static AsyncOperationHandle<{collection.GetType().FullName}> collectionHandle;");
                AppendLine(writer, indentation, $"public static AsyncOperationHandle<{collection.GetType().FullName}> LoadCollectionAsync()");
                AppendLine(writer, indentation, "{");
                indentation++;
                AppendLine(writer, indentation, $"collectionHandle = Addressables.LoadAssetAsync<{collection.GetType().FullName}>(\"{entry.guid}\");");
                AppendLine(writer, indentation, "collectionHandle.Completed += operation =>");
                AppendLine(writer, indentation, "{");
                indentation++;
                AppendLine(writer, indentation, "CollectionsRegistry.Instance.RegisterCollection(operation.Result);");
                AppendLine(writer, indentation, $"{HasCachedValuesName} = true;");
                AppendLine(writer, indentation, $"{PrivateValuesName} = operation.Result;");
                indentation--;
                AppendLine(writer, indentation, "};");
                AppendLine(writer, indentation, "return collectionHandle;");
                indentation--;
                AppendLine(writer, indentation, "}");
                AppendLine(writer, indentation);
                
                AppendLine(writer, indentation, "public static void UnloadCollection()");
                AppendLine(writer, indentation, "{");
                indentation++;
                AppendLine(writer, indentation, $"CollectionsRegistry.Instance.UnregisterCollection({PublicValuesName});");
                AppendLine(writer, indentation, $"{HasCachedValuesName} = false;");
                AppendLine(writer, indentation, $"{PrivateValuesName} = null;");

                AppendLine(writer, indentation, "Addressables.Release(collectionHandle);");
                indentation--;
                AppendLine(writer, indentation, "}");
                
            }
#endif
        }


        public static bool DoesStaticFileForCollectionExist(ScriptableObjectCollection collection)
        {
            return File.Exists(Path.Combine(
                AssetDatabase.GetAssetPath(SOCSettings.Instance.GetParentDefaultAssetScriptsFolderForCollection(collection)),
                $"{SOCSettings.Instance.GetStaticFilenameForCollection(collection)}{ExtensionNew}"));
        }
    }
}
