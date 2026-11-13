namespace BrunoMikoski.ScriptableObjectCollections.Picker
{
    /// <summary>
    /// Single source of truth for evaluating one <c>CollectionItemQuery&lt;T&gt;.MatchType</c> rule.
    /// Non-generic (and int-typed) so editor validation and tests can share the exact same
    /// semantics as the runtime match paths without closing the generic query type.
    /// </summary>
    public static class CollectionItemQueryRule
    {
        /// <summary>Mirrors <c>MatchType.Any</c>.</summary>
        public const int Any = 0;
        /// <summary>Mirrors <c>MatchType.All</c>.</summary>
        public const int All = 1;
        /// <summary>Mirrors <c>MatchType.NotAny</c>.</summary>
        public const int NotAny = 2;
        /// <summary>Mirrors <c>MatchType.NotAll</c>.</summary>
        public const int NotAll = 3;
        /// <summary>Mirrors <c>MatchType.AtLeast</c>.</summary>
        public const int AtLeast = 4;

        /// <summary>
        /// Evaluates one rule given how many of the rule's picker items were found on the target
        /// (<paramref name="matchCount"/>) out of the picker's total (<paramref name="pickerCount"/>).
        /// Callers are expected to skip rules with <paramref name="pickerCount"/> == 0 (inert rules)
        /// before calling; passing 0 evaluates the raw quantifier semantics.
        /// <paramref name="minimumCount"/> is only read by <see cref="AtLeast"/>. Values below 1 count
        /// as 1, because a rule added in the inspector can start zeroed.
        /// </summary>
        public static bool Passes(int matchType, int matchCount, int pickerCount, int minimumCount = 1)
        {
            switch (matchType)
            {
                case NotAny:
                    return matchCount == 0;
                case NotAll:
                    return matchCount != pickerCount;
                case Any:
                    return matchCount > 0;
                case All:
                    return matchCount >= pickerCount;
                case AtLeast:
                    return matchCount >= System.Math.Max(minimumCount, 1);
                default:
                    return true;
            }
        }
    }
}
