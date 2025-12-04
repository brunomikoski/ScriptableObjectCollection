using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public static class StringExtensions
    {
        static readonly Regex INVALID_CHARS_RGX = new Regex("[^_a-zA-Z0-9]");
        static readonly Regex WHITE_SPACE = new Regex(@"(?<=\s)");
        static readonly Regex STARTS_WITH_LOWER_CASE_CHAR = new Regex("^[a-z]");
        static readonly Regex FIRST_CHAR_FOLLOWED_BY_UPPER_CASES_ONLY = new Regex("(?<=[A-Z])[A-Z0-9]+$");
        static readonly Regex LOWER_CASE_NEXT_TO_NUMBER = new Regex("(?<=[0-9])[a-z]");
        static readonly Regex UPPER_CASE_INSIDE = new Regex("(?<=[A-Z])[A-Z]+?((?=[A-Z][a-z])|(?=[0-9]))");
        
        private const string HUNGARIAN_PREFIX = "m_";
        private const char UNDERSCORE = '_';
        private const char DEFAULT_SEPARATOR = ' ';
        
        private static readonly char[] DIRECTORY_SEPARATORS = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        private const string ASSETS_FOLDER = "Assets";
        
        private static readonly string[] RESERVED_KEYWORDS = {
            "abstract", "as", "base", " bool", " break", "byte", "case", "catch", "char", "checked", "class", "const",
            "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
            "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
            "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
            "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
            "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while", "add",
            "alias", "ascending", "async", "await", "by", "descending", "dynamic", "equals", "from", "get", "global",
            "group", "into", "join", "let", "nameof", "notnull", "on", "orderby", "partial", "partial", "remove",
            "select", "set", "unmanaged", "value", "var", "when", "where", "where", "with", "yield","values"
        };
        
        private static readonly char[] EXTRA_INVALID_FILE_CHARS = { '`' };

        private static readonly string[] WINDOWS_RESERVED_NAMES =
        {
            "CON","PRN","AUX","NUL","COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
            "LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"
        };

        public static string Sanitize(this string input)
        {
            // Treat any non-alphanumeric sequence as a word separator
            input = Regex.Replace(input, "[^a-zA-Z0-9]+", "_");

            IEnumerable<string> pascalCase =
                input
                    .Trim('_')
                    // split by underscores
                    .Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
                    // set first letter to uppercase
                    .Select(w => STARTS_WITH_LOWER_CASE_CHAR.Replace(w, m => m.Value.ToUpper()))
                    // replace second and all following upper case letters to lower if there is no next lower (ABC -> Abc)
                    .Select(w => FIRST_CHAR_FOLLOWED_BY_UPPER_CASES_ONLY.Replace(w, m => m.Value.ToLower()))
                    // set upper case the first lower case following a number (Ab9cd -> Ab9Cd)
                    .Select(w => LOWER_CASE_NEXT_TO_NUMBER.Replace(w, m => m.Value.ToUpper()))
                    // lower second and next upper case letters except the last if it follows by any lower (ABcDEf -> AbcDef)
                    .Select(w => UPPER_CASE_INSIDE.Replace(w, m => m.Value.ToLower()));

            return string.Concat(pascalCase);
        }

        public static string StartingNumbersToWords(this string input)
        {
            input = INVALID_CHARS_RGX.Replace(input, "");
            StringBuilder targetNumberString = new StringBuilder();
            int endIndex = 0;
            bool needToConvert = false;
            bool allDigits = true;
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (char.IsDigit(c))
                {
                    targetNumberString.Append(c);
                    needToConvert = true;
                }
                else
                {
                    endIndex = i;
                    allDigits = false;
                    break;
                }
            }

            if (!needToConvert)
                return input;

            input = input.Substring(endIndex);
            
            int targetNumberValue = Int32.Parse(targetNumberString.ToString());

            string numberToWords = NumberToWords(targetNumberValue);
            if (allDigits)
                return numberToWords;

            return numberToWords + input.FirstToUpper();
        }

        private static string NumberToWords(int number)
        {
            if (number == 0)
                return "zero";

            if (number < 0)
                return "minus " + NumberToWords(Math.Abs(number));

            string words = "";

            if ((number / 1000000) > 0)
            {
                words += NumberToWords(number / 1000000) + " million ";
                number %= 1000000;
            }

            if ((number / 1000) > 0)
            {
                words += NumberToWords(number / 1000) + " thousand ";
                number %= 1000;
            }

            if ((number / 100) > 0)
            {
                words += NumberToWords(number / 100) + " hundred ";
                number %= 100;
            }

            if (number > 0)
            {
                if (words != "")
                    words += "and ";

                string[] unitsMap = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
                string[] tensMap = { "zero", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

                if (number < 20)
                    words += unitsMap[number];
                else
                {
                    words += tensMap[number / 10];
                    if ((number % 10) > 0)
                        words += " " + unitsMap[number % 10];
                }
            }

            return words;
        }
        
        public static string FirstToLower(this string input)
        {
            return char.ToLower(input[0]) + input.Substring(1);
        }

        public static string FirstToUpper(this string input)
        {
            return char.ToUpper(input[0]) + input.Substring(1);
        }
        
        public static bool IsReservedKeyword(this string targetName)
        {
            for (int i = 0; i < RESERVED_KEYWORDS.Length; i++)
            {
                string reservedKeyword = RESERVED_KEYWORDS[i];
                if (reservedKeyword.Equals(targetName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
        
        private static bool IsExcludedSymbol(char symbol, char wordSeparator = DEFAULT_SEPARATOR)
        {
            return char.IsWhiteSpace(symbol) || char.IsPunctuation(symbol) || symbol == wordSeparator;
        }

        /// <summary>
        /// Gets the human readable version of programmer text, like a variable name.
        /// </summary>
        /// <param name="programmerText">The programmer text.</param>
        /// <returns>The human readable equivalent of the programmer text.</returns>
        public static string ToHumanReadable(this string programmerText, char wordSeparator = DEFAULT_SEPARATOR)
        {
            if (string.IsNullOrEmpty(programmerText))
                return programmerText;

            bool wasLetter = false;
            bool wasUpperCase = false;
            bool addedSpace = false;
            string result = "";

            // First remove the m_ prefix if it exists.
            if (programmerText.StartsWith(HUNGARIAN_PREFIX))
                programmerText = programmerText.Substring(HUNGARIAN_PREFIX.Length);

            // Deal with any miscellanneous spaces.
            if (wordSeparator != DEFAULT_SEPARATOR)
                programmerText = programmerText.Replace(DEFAULT_SEPARATOR, wordSeparator);

            // Deal with any miscellanneous underscores.
            if (wordSeparator != UNDERSCORE)
                programmerText = programmerText.Replace(UNDERSCORE, wordSeparator);

            // Go through the original string and copy it with some modifications.
            for (int i = 0; i < programmerText.Length; i++)
            {
                // If there was a change in caps add spaces.
                if ((wasUpperCase != char.IsUpper(programmerText[i])
                     || (wasLetter != char.IsLetter(programmerText[i])))
                    && i > 0 && !addedSpace
                    && !(IsExcludedSymbol(programmerText[i], wordSeparator) ||
                         IsExcludedSymbol(programmerText[i - 1], wordSeparator)))
                {
                    // Upper case to lower case.
                    // I added this so that something like 'GUIItem' turns into 'GUI Item', but that 
                    // means we have to make sure that no symbols are involved. Also check that there 
                    // isn't already a space where we want to add a space. Don't want to double space.
                    if (wasUpperCase && i > 1 && !IsExcludedSymbol(programmerText[i - 1], wordSeparator)
                        && !IsExcludedSymbol(result[result.Length - 2], wordSeparator))
                    {
                        // From letter to letter means we have to insert a space one character back.
                        // Otherwise it's going from a letter to a symbol and we can just add a space.
                        if (wasLetter && char.IsLetter(programmerText[i]))
                            result = result.Insert(result.Length - 1, wordSeparator.ToString());
                        else
                            result += wordSeparator;
                        addedSpace = true;
                    }

                    // Lower case to upper case.
                    if (!wasUpperCase)
                    {
                        result += wordSeparator;
                        addedSpace = true;
                    }
                }
                else
                {
                    // No case change.
                    addedSpace = false;
                }

                // Add the character.
                result += programmerText[i];

                // Capitalize the first character.
                if (i == 0)
                    result = result.ToUpper();

                // Remember things about the previous letter.
                wasLetter = char.IsLetter(programmerText[i]);
                wasUpperCase = char.IsUpper(programmerText[i]);
            }

            return result;
        }

        public static string ToPathWithConsistentSeparators(this string path)
        {
            return path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        
        public static bool HasParentDirectory(this string path)
        {
            return path.LastIndexOfAny(DIRECTORY_SEPARATORS) != -1;
        }
    
        public static string GetParentDirectory(this string path)
        {
            int lastDirectorySeparator = path.LastIndexOfAny(DIRECTORY_SEPARATORS);
            if (lastDirectorySeparator == -1)
                return path;

            return path.Substring(0, lastDirectorySeparator);
        }
        
        public static bool StartsWithAny(this string path, params string[] prefixes)
        {
            foreach (string prefix in prefixes)
            {
                if (path.StartsWith(prefix))
                    return true;
            }

            return false;
        }
        
        public static string RemovePrefix(this string name, string prefix)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(prefix))
                return name;

            if (!name.StartsWith(prefix))
                return name;

            return name.Substring(prefix.Length);
        }
    
        public static string RemovePrefix(this string name, char prefix)
        {
            return RemovePrefix(name, prefix.ToString());
        }
        
        public static string RemoveSuffix(this string name, string suffix)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(suffix))
                return name;

            if (!name.EndsWith(suffix))
                return name;

            return name.Substring(0, name.Length - suffix.Length);
        }
    
        public static string RemoveSuffix(this string name, char suffix)
        {
            return RemoveSuffix(name, suffix.ToString());
        }
        
        private static string RemoveAssetsPrefix(this string path)
        {
            return RemovePrefix(path.ToPathWithConsistentSeparators(), ASSETS_FOLDER + Path.AltDirectorySeparatorChar);
        }
        
        public static string GetAbsolutePath(this string projectPath)
        {
            string absolutePath = RemoveAssetsPrefix(projectPath);
            return Application.dataPath + Path.AltDirectorySeparatorChar + absolutePath;
        }
    
        public static string GetProjectPath(this string absolutePath)
        {
            absolutePath = absolutePath.ToPathWithConsistentSeparators();
            string projectPath = RemoveSuffix(Application.dataPath, ASSETS_FOLDER);
            projectPath = RemoveSuffix(projectPath, Path.AltDirectorySeparatorChar);
            
            string relativePath = ToPathWithConsistentSeparators(Path.GetRelativePath(projectPath, absolutePath));
            return relativePath;
        }
        
        public static bool IsValidFilename(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            char[] invalid = Path.GetInvalidFileNameChars();
            if (value.IndexOfAny(invalid) >= 0)
                return false;

            if (value.IndexOfAny(EXTRA_INVALID_FILE_CHARS) >= 0)
                return false;

            if (value.EndsWith(" ") || value.EndsWith("."))
                return false;

            string stem = Path.GetFileNameWithoutExtension(value);
            if (WINDOWS_RESERVED_NAMES.Any(r => r.Equals(stem, StringComparison.OrdinalIgnoreCase)))
                return false;

            return true;
        }
        
        public static string ToValidFilename(this string value, char replacement = '_')
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (value.IsValidFilename())
                return value;

            string safe = value;

            foreach (char c in Path.GetInvalidFileNameChars())
                safe = safe.Replace(c, replacement);

            foreach (char c in EXTRA_INVALID_FILE_CHARS)
                safe = safe.Replace(c, replacement);

            safe = safe.TrimEnd(' ', '.');

            string stem = Path.GetFileNameWithoutExtension(safe);
            if (WINDOWS_RESERVED_NAMES.Any(r => r.Equals(stem, StringComparison.OrdinalIgnoreCase)))
                safe = "_" + safe;

            if (string.IsNullOrWhiteSpace(safe))
                safe = "_";

            return safe;
        }
    }
}
