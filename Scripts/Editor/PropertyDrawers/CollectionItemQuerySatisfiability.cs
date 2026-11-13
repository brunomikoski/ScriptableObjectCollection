using System.Collections.Generic;

namespace BrunoMikoski.ScriptableObjectCollections.Picker
{
    /// <summary>
    /// Exact satisfiability analysis for <c>CollectionItemQuery</c> rule sets, used by
    /// <see cref="CollectionItemQueryPropertyDrawer"/> to flag queries that can never match.
    ///
    /// A rule's outcome only depends on which of its own picker items are present on the target,
    /// so a query referencing k distinct items is satisfiable iff one of the 2^k presence
    /// assignments over those items passes every rule (evaluated with the exact runtime semantics,
    /// <see cref="CollectionItemQueryRule.Passes"/>). This is complete — it catches conflicts that
    /// pairwise reasoning provably cannot (e.g. Any{a} + Any{b} + NotAll{a,b}). Beyond
    /// <see cref="MaxExactItems"/> distinct items it degrades to a pairwise check that has zero
    /// false positives but may miss multi-rule conflicts.
    ///
    /// Rules whose item set is empty are ignored, mirroring the runtime, which skips them as inert.
    /// </summary>
    public static class CollectionItemQuerySatisfiability
    {
        public readonly struct Rule
        {
            /// <summary>One of the <see cref="CollectionItemQueryRule"/> constants.</summary>
            public readonly int MatchType;
            /// <summary>The rule's picker items as raw LongGuid value pairs.</summary>
            public readonly HashSet<(long, long)> Items;
            /// <summary>Only read by <see cref="CollectionItemQueryRule.AtLeast"/>.</summary>
            public readonly int MinimumCount;

            public Rule(int matchType, HashSet<(long, long)> items, int minimumCount = 1)
            {
                MatchType = matchType;
                Items = items;
                MinimumCount = minimumCount;
            }
        }

        /// <summary>Distinct-item ceiling for the exact 2^k check (65536 assignments).</summary>
        public const int MaxExactItems = 16;

        /// <summary>Rule-count ceiling for smallest-conflict attribution (2^6 subsets).</summary>
        public const int MaxAttributionRules = 6;

        private static readonly List<Rule> activeRulesBuffer = new List<Rule>();
        private static readonly Dictionary<(long, long), int> itemToBitBuffer = new Dictionary<(long, long), int>();
        private static readonly List<Rule> subsetBuffer = new List<Rule>();

        public static bool IsSatisfiable(IReadOnlyList<Rule> rules)
        {
            activeRulesBuffer.Clear();
            itemToBitBuffer.Clear();

            for (int i = 0; i < rules.Count; i++)
            {
                Rule rule = rules[i];
                if (rule.Items == null || rule.Items.Count == 0)
                    continue;

                activeRulesBuffer.Add(rule);
                foreach ((long, long) item in rule.Items)
                {
                    if (!itemToBitBuffer.ContainsKey(item))
                        itemToBitBuffer.Add(item, itemToBitBuffer.Count);
                }
            }

            if (activeRulesBuffer.Count == 0)
                return true;

            int itemCount = itemToBitBuffer.Count;
            if (itemCount > MaxExactItems)
                return !HasImpossibleRule(activeRulesBuffer) && !HasPairwiseConflict(activeRulesBuffer);

            int ruleCount = activeRulesBuffer.Count;
            ulong[] ruleMasks = new ulong[ruleCount];
            int[] pickerCounts = new int[ruleCount];
            int[] matchTypes = new int[ruleCount];
            int[] minimumCounts = new int[ruleCount];
            for (int i = 0; i < ruleCount; i++)
            {
                Rule rule = activeRulesBuffer[i];
                ulong mask = 0UL;
                foreach ((long, long) item in rule.Items)
                    mask |= 1UL << itemToBitBuffer[item];

                ruleMasks[i] = mask;
                pickerCounts[i] = rule.Items.Count;
                matchTypes[i] = rule.MatchType;
                minimumCounts[i] = rule.MinimumCount;
            }

            ulong assignmentCount = 1UL << itemCount;
            for (ulong assignment = 0; assignment < assignmentCount; assignment++)
            {
                bool allPass = true;
                for (int i = 0; i < ruleCount; i++)
                {
                    int matchCount = PopCount(assignment & ruleMasks[i]);
                    if (!CollectionItemQueryRule.Passes(matchTypes[i], matchCount, pickerCounts[i], minimumCounts[i]))
                    {
                        allPass = false;
                        break;
                    }
                }

                if (allPass)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// When the rule set is unsatisfiable, finds the smallest subset of rules that is already
        /// unsatisfiable on its own (for up to <see cref="MaxAttributionRules"/> rules) and writes
        /// their indices into <paramref name="resultRuleIndices"/>. Above the cap, all rule indices
        /// are returned. Returns false when the rule set is satisfiable.
        /// </summary>
        public static bool TryFindSmallestConflict(IReadOnlyList<Rule> rules, List<int> resultRuleIndices)
        {
            resultRuleIndices.Clear();
            if (IsSatisfiable(rules))
                return false;

            int ruleCount = rules.Count;
            if (ruleCount > MaxAttributionRules)
            {
                for (int i = 0; i < ruleCount; i++)
                    resultRuleIndices.Add(i);
                return true;
            }

            int subsetCount = 1 << ruleCount;
            for (int size = 1; size <= ruleCount; size++)
            {
                for (int subset = 1; subset < subsetCount; subset++)
                {
                    if (PopCount((ulong)subset) != size)
                        continue;

                    subsetBuffer.Clear();
                    for (int i = 0; i < ruleCount; i++)
                    {
                        if ((subset & (1 << i)) != 0)
                            subsetBuffer.Add(rules[i]);
                    }

                    if (IsSatisfiable(subsetBuffer))
                        continue;

                    for (int i = 0; i < ruleCount; i++)
                    {
                        if ((subset & (1 << i)) != 0)
                            resultRuleIndices.Add(i);
                    }

                    return true;
                }
            }

            // Unreachable: the full set is unsatisfiable, so size == ruleCount always matches.
            for (int i = 0; i < ruleCount; i++)
                resultRuleIndices.Add(i);
            return true;
        }

        // A rule that fails for every possible match count, such as AtLeast 3 of 2 items.
        private static bool HasImpossibleRule(List<Rule> rules)
        {
            for (int i = 0; i < rules.Count; i++)
            {
                Rule rule = rules[i];
                int pickerCount = rule.Items.Count;
                bool canPass = false;
                for (int matchCount = 0; matchCount <= pickerCount; matchCount++)
                {
                    if (!CollectionItemQueryRule.Passes(rule.MatchType, matchCount, pickerCount, rule.MinimumCount))
                        continue;

                    canPass = true;
                    break;
                }

                if (!canPass)
                    return true;
            }

            return false;
        }

        // Conservative fallback for queries referencing more than MaxExactItems distinct items:
        // detects only pairwise contradictions (proven to never flag a satisfiable query).
        private static bool HasPairwiseConflict(List<Rule> rules)
        {
            for (int i = 0; i < rules.Count; i++)
            {
                for (int j = i + 1; j < rules.Count; j++)
                {
                    if (!HasItemIntersection(rules[i].Items, rules[j].Items))
                        continue;

                    if (IsCombinationImpossible(rules[i], rules[j]))
                        return true;
                }
            }

            return false;
        }

        private static bool IsCombinationImpossible(Rule ruleA, Rule ruleB)
        {
            int matchA = ruleA.MatchType;
            int matchB = ruleB.MatchType;
            HashSet<(long, long)> itemsA = ruleA.Items;
            HashSet<(long, long)> itemsB = ruleB.Items;

            // Caller guarantees itemsA ∩ itemsB is non-empty.
            //
            // All + NotAny: the overlap is forced-in by All and forced-out by NotAny → always impossible.
            // Any(X) + NotAny(Y): impossible iff every X item is also forbidden by Y (X ⊆ Y).
            // All(X) + NotAll(Y): impossible iff every Y item is already required by X (Y ⊆ X).
            // Any(X) + NotAll(Y): impossible iff X == Y == a single shared item.
            // AtLeast N(X) + NotAny(Y): impossible iff fewer than N X items are outside Y.
            // AtLeast N(X) + NotAll(Y): impossible iff Y ⊆ X and |X| <= N, since one Y item must be missing.

            if (matchA == CollectionItemQueryRule.AtLeast)
                return IsAtLeastCombinationImpossible(ruleA, ruleB);
            if (matchB == CollectionItemQueryRule.AtLeast)
                return IsAtLeastCombinationImpossible(ruleB, ruleA);

            if ((matchA == CollectionItemQueryRule.All && matchB == CollectionItemQueryRule.NotAny) ||
                (matchA == CollectionItemQueryRule.NotAny && matchB == CollectionItemQueryRule.All))
                return true;

            if (matchA == CollectionItemQueryRule.Any && matchB == CollectionItemQueryRule.NotAny)
                return IsSubsetOf(itemsA, itemsB);
            if (matchA == CollectionItemQueryRule.NotAny && matchB == CollectionItemQueryRule.Any)
                return IsSubsetOf(itemsB, itemsA);

            if (matchA == CollectionItemQueryRule.All && matchB == CollectionItemQueryRule.NotAll)
                return IsSubsetOf(itemsB, itemsA);
            if (matchA == CollectionItemQueryRule.NotAll && matchB == CollectionItemQueryRule.All)
                return IsSubsetOf(itemsA, itemsB);

            if ((matchA == CollectionItemQueryRule.Any && matchB == CollectionItemQueryRule.NotAll) ||
                (matchA == CollectionItemQueryRule.NotAll && matchB == CollectionItemQueryRule.Any))
                return itemsA.Count == 1 && itemsB.Count == 1 && IsSubsetOf(itemsA, itemsB);

            return false;
        }

        private static bool IsAtLeastCombinationImpossible(Rule atLeastRule, Rule otherRule)
        {
            int minimumCount = System.Math.Max(atLeastRule.MinimumCount, 1);
            switch (otherRule.MatchType)
            {
                case CollectionItemQueryRule.NotAny:
                    return CountNotIn(atLeastRule.Items, otherRule.Items) < minimumCount;
                case CollectionItemQueryRule.NotAll:
                    if (!IsSubsetOf(otherRule.Items, atLeastRule.Items))
                        return false;

                    return atLeastRule.Items.Count <= minimumCount;
                default:
                    return false;
            }
        }

        private static int CountNotIn(HashSet<(long, long)> items, HashSet<(long, long)> excluded)
        {
            int count = 0;
            foreach ((long, long) item in items)
            {
                if (!excluded.Contains(item))
                    count++;
            }

            return count;
        }

        private static bool HasItemIntersection(HashSet<(long, long)> a, HashSet<(long, long)> b)
        {
            if (a == null || b == null || a.Count == 0 || b.Count == 0)
                return false;

            foreach ((long, long) item in a)
            {
                if (b.Contains(item))
                    return true;
            }

            return false;
        }

        private static bool IsSubsetOf(HashSet<(long, long)> candidate, HashSet<(long, long)> container)
        {
            if (candidate == null || container == null || candidate.Count == 0)
                return false;

            foreach ((long, long) item in candidate)
            {
                if (!container.Contains(item))
                    return false;
            }

            return true;
        }

        private static int PopCount(ulong x)
        {
            x = x - ((x >> 1) & 0x5555555555555555UL);
            x = (x & 0x3333333333333333UL) + ((x >> 2) & 0x3333333333333333UL);
            x = (x + (x >> 4)) & 0x0F0F0F0F0F0F0F0FUL;
            return (int)((x * 0x0101010101010101UL) >> 56);
        }
    }
}
