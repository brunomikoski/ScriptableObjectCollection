using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Rule = BrunoMikoski.ScriptableObjectCollections.Picker.CollectionItemQuerySatisfiability.Rule;

namespace BrunoMikoski.ScriptableObjectCollections.Picker
{
    [CustomPropertyDrawer(typeof(CollectionItemQuery<>), true)]
    public class CollectionItemQueryPropertyDrawer : PropertyDrawer
    {
        private const string QUERY_PROPERTY_NAME = "query";
        private const string MATCH_TYPE_PROPERTY_NAME = "matchType";
        private const string MINIMUM_COUNT_PROPERTY_NAME = "minimumCount";
        private const string PICKER_PROPERTY_NAME = "picker";
        private const string ITEMS_PROPERTY_NAME = "indirectReferences";
        private const string COLLECTION_ITEM_GUID_VALUE_A = "collectionItemGUIDValueA";
        private const string COLLECTION_ITEM_GUID_VALUE_B = "collectionItemGUIDValueB";
        private const string COLLECTION_GUID_VALUE_A = "collectionGUIDValueA";
        private const string COLLECTION_GUID_VALUE_B = "collectionGUIDValueB";

        private const string EMPTY_RULES_MESSAGE = "Rules with no items selected are ignored.";

        private const int MATCH_TYPE_COUNT = 5;

        private const float HELP_BOX_CALC_WIDTH_MARGIN = 48f;
        private const float MINIMUM_COUNT_WIDTH = 36f;

        private static readonly string[] MATCH_TYPE_HINTS =
        {
            "has at least one",
            "has every one",
            "has none",
            "missing at least one",
            "has at least N",
        };

        private float lastContentWidth;
        private bool hasValidationCache;
        private int cachedValidationHash;
        private readonly List<Rule> cachedRules = new List<Rule>();
        private readonly List<int> cachedValidTypeMasks = new List<int>();
        private bool cachedIsSatisfiable = true;
        private string cachedConflictMessage;
        private bool cachedHasEmptyRules;
        private string cachedSummary;
        private readonly List<int> conflictIndicesBuffer = new List<int>();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty queryProp = property.FindPropertyRelative(QUERY_PROPERTY_NAME);
            float height = EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded || queryProp == null)
                return height;

            EnsureValidation(property, queryProp);

            height += EditorGUIUtility.standardVerticalSpacing;

            for (int i = 0; i < queryProp.arraySize; i++)
            {
                SerializedProperty element = queryProp.GetArrayElementAtIndex(i);
                SerializedProperty pickerProp = element != null ? element.FindPropertyRelative(PICKER_PROPERTY_NAME) : null;

                float rowHeight = EditorGUIUtility.singleLineHeight;
                if (pickerProp != null)
                    rowHeight = Mathf.Max(rowHeight, EditorGUI.GetPropertyHeight(pickerProp, GUIContent.none, true));

                height += rowHeight + EditorGUIUtility.standardVerticalSpacing * 2f;
            }

            height += EditorGUIUtility.singleLineHeight +
                      EditorGUIUtility.standardVerticalSpacing;

            height += GetHelpBoxesHeight();

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty queryProp = property.FindPropertyRelative(QUERY_PROPERTY_NAME);

            Rect foldoutRect = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight);

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (property.isExpanded && queryProp != null)
            {
                EnsureValidation(property, queryProp);

                int previousIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = previousIndent + 1;
                Rect contentRect = EditorGUI.IndentedRect(new Rect(
                    position.x,
                    foldoutRect.yMax + EditorGUIUtility.standardVerticalSpacing,
                    position.width,
                    position.height));
                // The rows are placed by hand inside the indented rect, so the controls must not indent again.
                EditorGUI.indentLevel = 0;
                lastContentWidth = contentRect.width;

                Rect line = contentRect;

                int indexToRemove = -1;

                for (int i = 0; i < queryProp.arraySize; i++)
                {
                    SerializedProperty element = queryProp.GetArrayElementAtIndex(i);
                    SerializedProperty matchTypeProp = element.FindPropertyRelative(MATCH_TYPE_PROPERTY_NAME);
                    SerializedProperty minimumCountProp = element.FindPropertyRelative(MINIMUM_COUNT_PROPERTY_NAME);
                    SerializedProperty pickerProp = element.FindPropertyRelative(PICKER_PROPERTY_NAME);

                    float rowHeight = EditorGUIUtility.singleLineHeight;
                    if (pickerProp != null)
                        rowHeight = Mathf.Max(rowHeight, EditorGUI.GetPropertyHeight(pickerProp, GUIContent.none, true));

                    Rect rowRect = line;
                    rowRect.height = rowHeight;

                    float removeButtonWidth = 20f;
                    Rect removeRect = new Rect(
                        rowRect.xMax - removeButtonWidth,
                        rowRect.y,
                        removeButtonWidth,
                        EditorGUIUtility.singleLineHeight);

                    float matchWidth = 100f;
                    Rect matchRect = new Rect(
                        rowRect.x,
                        rowRect.y,
                        matchWidth,
                        EditorGUIUtility.singleLineHeight);

                    int validTypeMask = i < cachedValidTypeMasks.Count ? cachedValidTypeMasks[i] : ~0;
                    DrawConstrainedMatchType(matchRect, matchTypeProp, validTypeMask);

                    float pickerX = matchRect.xMax + 4f;
                    if (ShowsMinimumCount(matchTypeProp, minimumCountProp))
                    {
                        Rect minimumCountRect = new Rect(
                            pickerX,
                            rowRect.y,
                            MINIMUM_COUNT_WIDTH,
                            EditorGUIUtility.singleLineHeight);
                        DrawMinimumCount(minimumCountRect, minimumCountProp);
                        pickerX = minimumCountRect.xMax + 4f;
                    }

                    Rect pickerRect = new Rect(
                        pickerX,
                        rowRect.y,
                        removeRect.x - 2f - pickerX,
                        rowHeight);

                    if (pickerProp != null)
                        EditorGUI.PropertyField(pickerRect, pickerProp, GUIContent.none, true);

                    if (GUI.Button(removeRect, "-"))
                    {
                        indexToRemove = i;
                    }

                    line.y = rowRect.y + rowHeight + EditorGUIUtility.standardVerticalSpacing * 2f;
                }

                if (indexToRemove >= 0 && indexToRemove < queryProp.arraySize)
                {
                    queryProp.DeleteArrayElementAtIndex(indexToRemove);
                }

                Rect addButtonRect = new Rect(
                    line.x,
                    line.y,
                    contentRect.width,
                    EditorGUIUtility.singleLineHeight);

                if (GUI.Button(addButtonRect, "Add Rule"))
                {
                    int newIndex = queryProp.arraySize;
                    queryProp.arraySize++;
                    SerializedProperty newElement = queryProp.GetArrayElementAtIndex(newIndex);
                    SerializedProperty newMatchType = newElement.FindPropertyRelative(MATCH_TYPE_PROPERTY_NAME);
                    if (newMatchType != null)
                        newMatchType.enumValueIndex = 0; // default to first enum value

                    // Growing the array clones the previous element's serialized data; without
                    // this the new rule starts pre-filled with the previous rule's count and picker items.
                    SerializedProperty newMinimumCount = newElement.FindPropertyRelative(MINIMUM_COUNT_PROPERTY_NAME);
                    if (newMinimumCount != null)
                        newMinimumCount.intValue = 1;

                    SerializedProperty newPickerProp = newElement.FindPropertyRelative(PICKER_PROPERTY_NAME);
                    SerializedProperty newItemsProp = newPickerProp != null
                        ? newPickerProp.FindPropertyRelative(ITEMS_PROPERTY_NAME)
                        : null;
                    if (newItemsProp != null)
                        newItemsProp.arraySize = 0;
                }

                line.y += EditorGUIUtility.singleLineHeight +
                          EditorGUIUtility.standardVerticalSpacing;

                float calcWidth = GetHelpBoxCalcWidth();

                if (!cachedIsSatisfiable && !string.IsNullOrEmpty(cachedConflictMessage))
                {
                    Rect helpRect = line;
                    float helpHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(cachedConflictMessage), calcWidth);
                    helpRect.height = helpHeight;
                    EditorGUI.HelpBox(helpRect, cachedConflictMessage, MessageType.Error);
                    line.y += helpHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                if (cachedHasEmptyRules)
                {
                    Rect emptyRect = line;
                    float emptyHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(EMPTY_RULES_MESSAGE), calcWidth);
                    emptyRect.height = emptyHeight;
                    EditorGUI.HelpBox(emptyRect, EMPTY_RULES_MESSAGE, MessageType.Info);
                    line.y += emptyHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                if (!string.IsNullOrEmpty(cachedSummary))
                {
                    Rect summaryRect = line;
                    float summaryHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(cachedSummary), calcWidth);
                    summaryRect.height = summaryHeight;
                    EditorGUI.HelpBox(summaryRect, cachedSummary, MessageType.Info);
                    line.y += summaryHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                EditorGUI.indentLevel = previousIndent;
            }

            EditorGUI.EndProperty();
        }

        // Help box heights must use the width they are drawn at, which only OnGUI knows.
        // Before the first OnGUI, estimate it: currentViewWidth is only valid during a GUI event,
        // so use a fixed width for programmatic height queries (Event.current is null outside OnGUI).
        private float GetHelpBoxCalcWidth()
        {
            if (lastContentWidth > 0f)
                return lastContentWidth;

            if (Event.current == null)
                return 320f;

            return EditorGUIUtility.currentViewWidth - HELP_BOX_CALC_WIDTH_MARGIN;
        }

        private float GetHelpBoxesHeight()
        {
            float calcWidth = GetHelpBoxCalcWidth();
            float height = 0f;

            if (!cachedIsSatisfiable && !string.IsNullOrEmpty(cachedConflictMessage))
            {
                height += EditorStyles.helpBox.CalcHeight(new GUIContent(cachedConflictMessage), calcWidth) +
                          EditorGUIUtility.standardVerticalSpacing;
            }

            if (cachedHasEmptyRules)
            {
                height += EditorStyles.helpBox.CalcHeight(new GUIContent(EMPTY_RULES_MESSAGE), calcWidth) +
                          EditorGUIUtility.standardVerticalSpacing;
            }

            if (!string.IsNullOrEmpty(cachedSummary))
            {
                height += EditorStyles.helpBox.CalcHeight(new GUIContent(cachedSummary), calcWidth) +
                          EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }

        private void EnsureValidation(SerializedProperty property, SerializedProperty queryProp)
        {
            int contentHash = ComputeContentHash(property, queryProp);
            if (hasValidationCache && contentHash == cachedValidationHash)
                return;

            hasValidationCache = true;
            cachedValidationHash = contentHash;

            cachedRules.Clear();
            cachedValidTypeMasks.Clear();
            cachedIsSatisfiable = true;
            cachedConflictMessage = null;
            cachedHasEmptyRules = false;
            cachedSummary = string.Empty;

            int arraySize = queryProp.arraySize;
            for (int i = 0; i < arraySize; i++)
            {
                int matchType = 0;
                int minimumCount = 1;
                HashSet<(long, long)> items = null;
                if (TryGetElementAndItems(queryProp, i, out SerializedProperty element, out HashSet<(long, long)> elementItems))
                {
                    items = elementItems;
                    SerializedProperty matchTypeProp = element.FindPropertyRelative(MATCH_TYPE_PROPERTY_NAME);
                    if (matchTypeProp != null)
                        matchType = matchTypeProp.enumValueIndex;

                    SerializedProperty minimumCountProp = element.FindPropertyRelative(MINIMUM_COUNT_PROPERTY_NAME);
                    if (minimumCountProp != null)
                        minimumCount = minimumCountProp.intValue;
                }

                items ??= new HashSet<(long, long)>();
                if (items.Count == 0)
                    cachedHasEmptyRules = true;

                cachedRules.Add(new Rule(matchType, items, minimumCount));
            }

            cachedIsSatisfiable = CollectionItemQuerySatisfiability.IsSatisfiable(cachedRules);
            if (!cachedIsSatisfiable)
            {
                CollectionItemQuerySatisfiability.TryFindSmallestConflict(cachedRules, conflictIndicesBuffer);
                cachedConflictMessage = BuildConflictMessage(conflictIndicesBuffer, arraySize);
            }

            for (int i = 0; i < arraySize; i++)
            {
                Rule original = cachedRules[i];
                int validMask;
                if (original.Items.Count == 0)
                {
                    validMask = ~0; // inert rule; any MatchType is fine
                }
                else
                {
                    validMask = 0;
                    for (int candidate = 0; candidate < MATCH_TYPE_COUNT; candidate++)
                    {
                        cachedRules[i] = new Rule(candidate, original.Items, original.MinimumCount);
                        if (CollectionItemQuerySatisfiability.IsSatisfiable(cachedRules))
                            validMask |= 1 << candidate;
                    }

                    cachedRules[i] = original;
                }

                cachedValidTypeMasks.Add(validMask);
            }

            cachedSummary = BuildSummaryText(queryProp);
        }

        private static int ComputeContentHash(SerializedProperty property, SerializedProperty queryProp)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + property.propertyPath.GetHashCode();
                hash = hash * 31 + (property.serializedObject.targetObject != null
                    ? property.serializedObject.targetObject.GetEntityId().GetHashCode()
                    : 0);

                int arraySize = queryProp.arraySize;
                hash = hash * 31 + arraySize;

                for (int i = 0; i < arraySize; i++)
                {
                    SerializedProperty element = queryProp.GetArrayElementAtIndex(i);
                    if (element == null)
                        continue;

                    SerializedProperty matchTypeProp = element.FindPropertyRelative(MATCH_TYPE_PROPERTY_NAME);
                    hash = hash * 31 + (matchTypeProp != null ? matchTypeProp.enumValueIndex : -1);

                    SerializedProperty minimumCountProp = element.FindPropertyRelative(MINIMUM_COUNT_PROPERTY_NAME);
                    if (minimumCountProp != null)
                        hash = hash * 31 + minimumCountProp.intValue;

                    SerializedProperty pickerProp = element.FindPropertyRelative(PICKER_PROPERTY_NAME);
                    SerializedProperty itemsProp = pickerProp != null
                        ? pickerProp.FindPropertyRelative(ITEMS_PROPERTY_NAME)
                        : null;
                    if (itemsProp == null)
                        continue;

                    for (int j = 0; j < itemsProp.arraySize; j++)
                    {
                        SerializedProperty elem = itemsProp.GetArrayElementAtIndex(j);
                        if (elem == null)
                            continue;

                        SerializedProperty guidAProp = elem.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_A);
                        SerializedProperty guidBProp = elem.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_B);
                        if (guidAProp == null || guidBProp == null)
                            continue;

                        hash = hash * 31 + guidAProp.longValue.GetHashCode();
                        hash = hash * 31 + guidBProp.longValue.GetHashCode();
                    }
                }

                return hash;
            }
        }

        private static string BuildConflictMessage(List<int> conflictIndices, int totalRules)
        {
            bool attributed = conflictIndices.Count > 0 && conflictIndices.Count < totalRules ||
                              (conflictIndices.Count == totalRules && totalRules <= CollectionItemQuerySatisfiability.MaxAttributionRules);

            if (!attributed || conflictIndices.Count == 0)
                return "This query contains rules that can never all be satisfied — it will never match any object.";

            if (conflictIndices.Count == 1)
                return $"Rule {conflictIndices[0] + 1} can never be satisfied — the query will never match any object.";

            StringBuilder stringBuilder = new StringBuilder("Rules ");
            for (int i = 0; i < conflictIndices.Count; i++)
            {
                if (i > 0)
                    stringBuilder.Append(i == conflictIndices.Count - 1 ? " and " : ", ");
                stringBuilder.Append(conflictIndices[i] + 1);
            }

            stringBuilder.Append(" can never all be satisfied — the query will never match any object.");
            return stringBuilder.ToString();
        }

        private void DrawConstrainedMatchType(Rect position, SerializedProperty matchTypeProp, int validTypeMask)
        {
            if (matchTypeProp == null)
            {
                EditorGUI.LabelField(position, "Invalid MatchType");
                return;
            }

            string[] allNames = matchTypeProp.enumDisplayNames;
            int currentEnumIndex = matchTypeProp.enumValueIndex;

            GUIContent buttonContent = new GUIContent("—");
            if (!matchTypeProp.hasMultipleDifferentValues && currentEnumIndex >= 0 && currentEnumIndex < allNames.Length)
                buttonContent = new GUIContent(allNames[currentEnumIndex], GetMatchTypeHint(currentEnumIndex));

            // The closed button shows only the name, because the hints do not fit its width.
            // The menu lists every match type with its hint and greys out the ones that would never match.
            EditorGUI.BeginProperty(position, GUIContent.none, matchTypeProp);
            if (EditorGUI.DropdownButton(position, buttonContent, FocusType.Keyboard, EditorStyles.popup))
                ShowMatchTypeMenu(position, matchTypeProp, allNames, validTypeMask);

            EditorGUI.EndProperty();
        }

        private static void ShowMatchTypeMenu(Rect position, SerializedProperty matchTypeProp, string[] allNames, int validTypeMask)
        {
            SerializedObject serializedObject = matchTypeProp.serializedObject;
            string propertyPath = matchTypeProp.propertyPath;
            bool hasMixedValues = matchTypeProp.hasMultipleDifferentValues;
            int currentEnumIndex = matchTypeProp.enumValueIndex;

            GenericMenu menu = new GenericMenu();
            for (int enumIndex = 0; enumIndex < allNames.Length; enumIndex++)
            {
                bool isCurrent = !hasMixedValues && enumIndex == currentEnumIndex;
                GUIContent itemContent = new GUIContent(GetFriendlyMatchTypeName(allNames, enumIndex));
                bool isValid = enumIndex >= MATCH_TYPE_COUNT || (validTypeMask & (1 << enumIndex)) != 0;
                if (!isValid)
                {
                    menu.AddDisabledItem(itemContent, isCurrent);
                    continue;
                }

                int selectedEnumIndex = enumIndex;
                menu.AddItem(itemContent, isCurrent, () =>
                {
                    serializedObject.Update();
                    serializedObject.FindProperty(propertyPath).enumValueIndex = selectedEnumIndex;
                    serializedObject.ApplyModifiedProperties();
                });
            }

            menu.DropDown(position);
        }

        private static bool ShowsMinimumCount(SerializedProperty matchTypeProp, SerializedProperty minimumCountProp)
        {
            if (matchTypeProp == null)
                return false;

            if (minimumCountProp == null)
                return false;

            return matchTypeProp.enumValueIndex == CollectionItemQueryRule.AtLeast;
        }

        private static void DrawMinimumCount(Rect position, SerializedProperty minimumCountProp)
        {
            EditorGUI.BeginProperty(position, GUIContent.none, minimumCountProp);
            EditorGUI.showMixedValue = minimumCountProp.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int newValue = EditorGUI.IntField(position, Mathf.Max(minimumCountProp.intValue, 1));
            if (EditorGUI.EndChangeCheck())
                minimumCountProp.intValue = Mathf.Max(newValue, 1);

            EditorGUI.showMixedValue = false;
            EditorGUI.EndProperty();
        }

        private static string GetFriendlyMatchTypeName(string[] enumDisplayNames, int enumIndex)
        {
            string baseName = enumDisplayNames[enumIndex];
            if (enumIndex < MATCH_TYPE_HINTS.Length)
                return $"{baseName} ({MATCH_TYPE_HINTS[enumIndex]})";

            return baseName;
        }

        private static string GetMatchTypeHint(int enumIndex)
        {
            if (enumIndex < MATCH_TYPE_HINTS.Length)
                return MATCH_TYPE_HINTS[enumIndex];

            return string.Empty;
        }

        private string BuildSummaryText(SerializedProperty queryProp)
        {
            if (queryProp == null || queryProp.arraySize == 0)
                return string.Empty;

            List<string> parts = new List<string>();

            for (int i = 0; i < queryProp.arraySize; i++)
            {
                if (!TryGetElementAndItems(queryProp, i, out SerializedProperty element, out HashSet<(long, long)> items))
                    continue;

                List<string> itemNames = GetItemNamesFromElement(element);
                if (itemNames.Count == 0)
                    continue;

                SerializedProperty matchTypeProp = element.FindPropertyRelative(MATCH_TYPE_PROPERTY_NAME);
                if (matchTypeProp == null)
                    continue;

                int minimumCount = 1;
                SerializedProperty minimumCountProp = element.FindPropertyRelative(MINIMUM_COUNT_PROPERTY_NAME);
                if (minimumCountProp != null)
                    minimumCount = Mathf.Max(minimumCountProp.intValue, 1);

                string ruleDescription = DescribeRule(matchTypeProp.enumValueIndex, itemNames, minimumCount);
                if (!string.IsNullOrEmpty(ruleDescription))
                    parts.Add(ruleDescription);
            }

            if (parts.Count == 0)
                return string.Empty;

            return "This query: " + string.Join(" and ", parts) + ".";
        }

        private static string DescribeRule(int matchType, List<string> itemNames, int minimumCount)
        {
            if (itemNames.Count == 1)
                return DescribeSingleItemRule(matchType, itemNames[0], minimumCount);

            string joinedNames = "{" + string.Join(", ", itemNames) + "}";
            switch (matchType)
            {
                case CollectionItemQueryRule.Any:
                    return $"allows objects that contain at least one of {joinedNames}";
                case CollectionItemQueryRule.All:
                    return $"requires objects to contain all of {joinedNames}";
                case CollectionItemQueryRule.NotAny:
                    return $"forbids objects that contain any of {joinedNames}";
                case CollectionItemQueryRule.NotAll:
                    return $"forbids objects that contain all of {joinedNames} together";
                case CollectionItemQueryRule.AtLeast:
                    return $"requires objects to contain at least {minimumCount} of {joinedNames}";
                default:
                    return null;
            }
        }

        // Singular phrasing: for one item, Any, All and AtLeast 1 mean "must contain it",
        // and NotAny and NotAll both mean "must not contain it".
        private static string DescribeSingleItemRule(int matchType, string itemName, int minimumCount)
        {
            switch (matchType)
            {
                case CollectionItemQueryRule.Any:
                case CollectionItemQueryRule.All:
                    return $"requires objects to contain {itemName}";
                case CollectionItemQueryRule.NotAny:
                case CollectionItemQueryRule.NotAll:
                    return $"forbids objects that contain {itemName}";
                case CollectionItemQueryRule.AtLeast:
                    if (minimumCount > 1)
                        return $"requires objects to contain at least {minimumCount} of {{{itemName}}}";

                    return $"requires objects to contain {itemName}";
                default:
                    return null;
            }
        }

        private bool TryGetElementAndItems(
            SerializedProperty queryProp,
            int index,
            out SerializedProperty element,
            out HashSet<(long, long)> items)
        {
            element = null;
            items = null;

            if (queryProp == null || index < 0 || index >= queryProp.arraySize)
                return false;

            element = queryProp.GetArrayElementAtIndex(index);
            if (element == null)
                return false;

            SerializedProperty pickerProp = element.FindPropertyRelative(PICKER_PROPERTY_NAME);
            if (pickerProp == null)
                return false;

            items = new HashSet<(long, long)>();
            SerializedProperty itemsProp = pickerProp.FindPropertyRelative(ITEMS_PROPERTY_NAME);
            if (itemsProp == null)
                return true; // no items, but still valid

            for (int i = 0; i < itemsProp.arraySize; i++)
            {
                SerializedProperty elem = itemsProp.GetArrayElementAtIndex(i);
                if (elem == null)
                    continue;

                SerializedProperty guidAProp = elem.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_A);
                SerializedProperty guidBProp = elem.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_B);

                if (guidAProp == null || guidBProp == null)
                    continue;

                long a = guidAProp.longValue;
                long b = guidBProp.longValue;

                // Never-assigned entries serialize as zeroed GUIDs; the runtime cannot resolve
                // them, so validation must not treat them as real items either.
                if (a == 0 && b == 0)
                    continue;

                items.Add((a, b));
            }

            return true;
        }

        private List<string> GetItemNamesFromElement(SerializedProperty element)
        {
            List<string> result = new List<string>();
            if (element == null)
                return result;

            SerializedProperty pickerProp = element.FindPropertyRelative(PICKER_PROPERTY_NAME);
            if (pickerProp == null)
                return result;

            SerializedProperty itemsProp = pickerProp.FindPropertyRelative(ITEMS_PROPERTY_NAME);
            if (itemsProp == null)
                return result;

            for (int i = 0; i < itemsProp.arraySize; i++)
            {
                SerializedProperty elem = itemsProp.GetArrayElementAtIndex(i);
                if (elem == null)
                    continue;

                SerializedProperty collectionGuidAProp = elem.FindPropertyRelative(COLLECTION_GUID_VALUE_A);
                SerializedProperty collectionGuidBProp = elem.FindPropertyRelative(COLLECTION_GUID_VALUE_B);
                SerializedProperty itemGuidAProp = elem.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_A);
                SerializedProperty itemGuidBProp = elem.FindPropertyRelative(COLLECTION_ITEM_GUID_VALUE_B);

                if (collectionGuidAProp == null || collectionGuidBProp == null ||
                    itemGuidAProp == null || itemGuidBProp == null)
                    continue;

                LongGuid collectionGuid = new LongGuid(collectionGuidAProp.longValue, collectionGuidBProp.longValue);
                LongGuid itemGuid = new LongGuid(itemGuidAProp.longValue, itemGuidBProp.longValue);

                if (!collectionGuid.IsValid() || !itemGuid.IsValid())
                    continue;

                if (!CollectionsRegistry.Instance.TryGetCollectionByGUID(collectionGuid, out ScriptableObjectCollection collection))
                    continue;

                if (!collection.TryGetItemByGUID(itemGuid, out ScriptableObject item))
                    continue;

                if (item != null && !string.IsNullOrEmpty(item.name))
                    result.Add(item.name);
            }

            return result;
        }
    }
}
