namespace BrunoMikoski.ScriptableObjectCollections.Utils
{
    public static class StringExtensions 
    {
        public static string AsBackingField(this string value)
        {
            return $"<{value}>k__BackingField";
        }
    }
}
