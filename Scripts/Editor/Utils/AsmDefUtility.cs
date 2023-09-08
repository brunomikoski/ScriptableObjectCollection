using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.ScriptableObjectCollections
{
    /// <summary>
    /// Contains useful utilities for organizing asmdefs.
    /// </summary>
    public static class AsmDefUtility 
    {
        private const char Separator = '.';
        
        [Serializable]
        private struct AsmRef
        {
            public string reference;

            public AsmRef(AssemblyDefinitionAsset reference)
            {
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(reference));
                this.reference = $"GUID:{guid}";
            }

            public override string ToString()
            {
                return JsonUtility.ToJson(this, true);
            }
        }

        [Serializable]
        private struct AsmDef
        {
            public string name;
            public string rootNamespace;
            public string[] references;
            public string[] includePlatforms;
            public string[] excludePlatforms;
            public bool allowUnsafeCode;
            public bool overrideReferences;
            public string[] precompiledReferences;
            public bool autoReferenced;
            public string[] defineConstraints;
            public string[] versionDefines;
            public bool noEngineReferences;

            public AsmDef(string name, params AssemblyDefinitionAsset[] references)
            {
                this.name = name;
                rootNamespace = "";
                this.references = new string[references.Length];
                for (int i = 0; i < references.Length; i++)
                {
                    string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(references[i]));
                    this.references[i] = $"GUID:{guid}";
                }
                includePlatforms = new string[0];
                excludePlatforms = new string[0];
                allowUnsafeCode = false;
                overrideReferences = false;
                precompiledReferences = new string[0];
                autoReferenced = true;
                defineConstraints = new string[0];
                versionDefines = new string[0];
                noEngineReferences = false;
            }

            public override string ToString()
            {
                return JsonUtility.ToJson(this, true);
            }
        }
        
        private static List<Object> GetSelectedEditorFolders()
        {
            List<Object> results = new List<Object>();
            for (int i = 0; i < Selection.objects.Length; i++)
            {
                if (Selection.objects[i].name == "Editor")
                    results.Add(Selection.objects[i]);
            }

            return results;
        }

        public static AssemblyDefinitionAsset GetParentEditorAsmDef(string path)
        {
            string currentFolder = path.GetParentDirectory();

            while (currentFolder.HasParentDirectory())
            {
                string parent = currentFolder.GetParentDirectory();
                
                if (currentFolder == parent)
                {
                    // This directory is like Harry Potter because it has no more parents left!
                    break;
                }

                string editorFolderNextToParent = parent + Path.AltDirectorySeparatorChar + "Editor";

                // It existed! Let's check that it has an asmdef.
                if (AssetDatabase.IsValidFolder(editorFolderNextToParent))
                {
                    // Try to find asmdef files in this folder. Note that there can only be one or zero.
                    string[] asmdefFileResults = AssetDatabase.FindAssets("t:asmdef", new[] {editorFolderNextToParent});

                    // Check if one existed.
                    if (asmdefFileResults.Length == 1)
                    {
                        string asmdefFilePath = AssetDatabase.GUIDToAssetPath(asmdefFileResults[0]);
                        
                        // It existed! This folder is a valid asm def to reference!
                        return AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(asmdefFilePath);
                    }
                }
                
                currentFolder = parent;
            }

            return null;
        }

        private static string GetAsmRefFileName(string path, string asmDefPath)
        {
            // If the asmdef is at "Assets/ProjectName/Scripts/Editor" then we'd like to get the filename relative to
            // "Assets/ProjectName/Scripts". Then we can filter out the "Scripts" folder, add it to the name of the asmDef
            // we reference, and then we get a file in the same naming convention as the asmdef we reference. 
            string asmDefDirectory = Path.GetDirectoryName(asmDefPath).ToPathWithConsistentSeparators();
            string asmDefParentDirectory = asmDefDirectory.GetParentDirectory();

            path = Path.GetRelativePath(asmDefParentDirectory, path);

            // The name should basically just be the folder relative to the asmdef that's referenced. 
            string asmFileName = path.Replace(Path.DirectorySeparatorChar, Separator)
                .Replace(Path.AltDirectorySeparatorChar, Separator);

            // Sometimes people add special characters so it shows up at the top. We don't want that for our filename,
            // so strip those out. Hyphens and spaces don't look nice either.
            string[] specialChars = { "_", "[", "]", "-", " " };
            foreach (string specialChar in specialChars)
            {
                asmFileName = asmFileName.Replace(specialChar, "");
            }
            
            // Remove any script folders from the name.
            string[] scriptFolderNames = {"Scripts", "Runtime"};
            List<string> segments = new List<string>(asmFileName.Split(Separator));
            while (segments.Count > 0 && segments[0].StartsWithAny(scriptFolderNames))
            {
                segments.RemoveAt(0);
            }
            asmFileName = string.Join(Separator, segments);

            string fileNameBase = Path.GetFileNameWithoutExtension(asmDefPath).RemoveSuffix(Separator + "Editor");
            string fileNameFinal = fileNameBase + Separator + asmFileName;
            return fileNameFinal;
        }

        public static void CreateAsmRef(string folderPath, AssemblyDefinitionAsset asmDef)
        {
            string asmDefPath = AssetDatabase.GetAssetPath(asmDef);
            string fileName = GetAsmRefFileName(folderPath, asmDefPath);
            string filePath = folderPath.GetAbsolutePath() + Path.AltDirectorySeparatorChar + fileName + ".asmref";

            AsmRef asmRef = new AsmRef(asmDef);
            
            File.WriteAllText(filePath, asmRef.ToString());
            AssetDatabase.ImportAsset(filePath.GetProjectPath());
        }

        public static AssemblyDefinitionAsset CreateEditorFolderAsmDef(
            string folderName, AssemblyDefinitionAsset runtimeAsmDef)
        {
            string asmDefPath = AssetDatabase.GetAssetPath(runtimeAsmDef);
            string fileName = Path.GetFileNameWithoutExtension(asmDefPath) + Separator + "Editor";
            string filePath = folderName.GetAbsolutePath() + Path.AltDirectorySeparatorChar + fileName + ".asmdef";

            AsmDef asmDef = new AsmDef(fileName, runtimeAsmDef);

            File.WriteAllText(filePath, asmDef.ToString());

            filePath = filePath.GetProjectPath();
            AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(filePath);
        }

        private static void AddAsmRefToTopLevelEditorFolder(Object selectedEditorFolder)
        {
            string path = AssetDatabase.GetAssetPath(selectedEditorFolder);
            AddAsmRefToTopLevelEditorFolder(path);
        }

        public static void AddAsmRefToTopLevelEditorFolder(string folderPath)
        {
            AssemblyDefinitionAsset editorAsmDefToReference = GetParentEditorAsmDef(folderPath);

            if (editorAsmDefToReference == null)
            {
                Debug.LogWarning($"Can't create asmref for folder {folderPath} because we can't find an editor folder asmdef.");
                return;
            }

            CreateAsmRef(folderPath, editorAsmDefToReference);
        }
        
        public static  AssemblyDefinitionAsset GetAsmDefInFolder(string path)
        {
            string[] asmDefsGuids = AssetDatabase.FindAssets("t:asmdef", new[] { path });
            for (int i = 0; i < asmDefsGuids.Length; i++)
            {
                string asmDefPath = AssetDatabase.GUIDToAssetPath(asmDefsGuids[i]);
                string asmDefDirectory = Path.GetDirectoryName(asmDefPath);
            
                if (asmDefDirectory == path)
                    return AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(asmDefPath);
            }

            return null;
        }
    
        public static AssemblyDefinitionAsset GetAsmDefInFolderOrParent(string path)
        {
            string currentPath = path;

            // First check if there's an asmdef in the start folder.
            AssemblyDefinitionAsset asmDefInStartFolder = GetAsmDefInFolder(path);
            if (asmDefInStartFolder != null)
                return asmDefInStartFolder;

            // Now keep checking every parent folder if there's an asmdef there.
            while (currentPath.HasParentDirectory())
            {
                // Go to the parent folder.
                currentPath = currentPath.GetParentDirectory();
            
                // See if there's an asmdef in this parent folder.
                AssemblyDefinitionAsset asmDefInFolder = GetAsmDefInFolder(currentPath);
                if (asmDefInFolder != null)
                    return asmDefInFolder;
            }

            return null;
        }

        public static AssemblyDefinitionAsset CreateEmptyEditorFolderForRuntimeAsmDef(
            AssemblyDefinitionAsset runtimeAsmDef, string dummyNamespace = null)
        {
            string runtimeAsmDefFolder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(runtimeAsmDef));

            AssetDatabase.CreateFolder(runtimeAsmDefFolder, "Editor");
            string editorFolder = runtimeAsmDefFolder + Path.AltDirectorySeparatorChar + "Editor";

            // Create a dummy file because you can't have asmdefs in empty folders.
            CreateDummyScript(dummyNamespace, editorFolder);

            return CreateEditorFolderAsmDef(editorFolder, runtimeAsmDef);
        }

        private static void CreateDummyScript(string dummyNamespace, string editorFolder)
        {
            bool hasNamespace = !string.IsNullOrEmpty(dummyNamespace);
            string dummyFilePath = editorFolder + Path.AltDirectorySeparatorChar + "Dummy.cs";

            StringBuilder sb = new StringBuilder();
            if (hasNamespace)
            {
                sb.AppendLine(dummyNamespace);
                sb.AppendLine("{");
                sb.Append("    ");
            }

            sb.AppendLine("public class Dummy {}");
            if (hasNamespace)
                sb.AppendLine("}\r\n");

            File.WriteAllText(dummyFilePath, sb.ToString());
            AssetDatabase.ImportAsset(dummyFilePath, ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
