using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections.Picker
{
    [CustomPropertyDrawer(typeof(CollectionItemQuery<>), true)]
    public class CollectionItemQueryPropertyDrawer : PropertyDrawer
    {
        private const string ITEMS_PROPERTY_NAME = "indirectReferences";
        private const string COLLECTION_ITEM_GUID_VALUE_A = "collectionItemGUIDValueA";
        private const string COLLECTION_ITEM_GUID_VALUE_B = "collectionItemGUIDValueB";
        private const string COLLECTION_GUID_VALUE_A = "collectionGUIDValueA";
        private const string COLLECTION_GUID_VALUE_B = "collectionGUIDValueB";

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty queryProp = property.FindPropertyRelative("query");
            float height = EditorGUIUtility.singleLineHeight; 

            if (!property.isExpanded || queryProp == null)
                return height;

            height += EditorGUIUtility.standardVerticalSpacing;

            for (int i = 0; i < queryProp.arraySize; i++)
            {
                SerializedProperty element = queryProp.GetArrayElementAtIndex(i);
                SerializedProperty pickerProp = element != null ? element.FindPropertyRelative("picker") : null;

                float rowHeight = EditorGUIUtility.singleLineHeight;
                if (pickerProp != null)
                    rowHeight = Mathf.Max(rowHeight, EditorGUI.GetPropertyHeight(pickerProp, GUIContent.none, true));

                height += rowHeight + EditorGUIUtility.standardVerticalSpacing * 2f;
            }

            height += EditorGUIUtility.singleLineHeight +
                      EditorGUIUtility.standardVerticalSpacing;

            if (HasImpossibleRules(queryProp))
            {
                string msg = "This query contains rules that can never be satisfied (conflicting MatchTypes for overlapping items).";
                float helpHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(msg), EditorGUIUtility.currentViewWidth - 32);
                height += helpHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            string summary = BuildSummaryText(queryProp);
            if (!string.IsNullOrEmpty(summary))
            {
                float summaryHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(summary), EditorGUIUtility.currentViewWidth - 32);
                height += summaryHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty queryProp = property.FindPropertyRelative("query");

            Rect foldoutRect = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight);

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (property.isExpanded && queryProp != null)
            {
                int previousIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = previousIndent + 1;
                Rect contentRect = EditorGUI.IndentedRect(new Rect(
                    position.x,
                    foldoutRect.yMax + EditorGUIUtility.standardVerticalSpacing,
                    position.width,
                    position.height));
                EditorGUI.indentLevel = previousIndent;

                Rect line = contentRect;

                int indexToRemove = -1;

                for (int i = 0; i < queryProp.arraySize; i++)
                {
                    SerializedProperty element = queryProp.GetArrayElementAtIndex(i);
                    SerializedProperty matchTypeProp = element.FindPropertyRelative("matchType");
                    SerializedProperty pickerProp = element.FindPropertyRelative("picker");

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

                    Rect pickerRect = new Rect(
                        matchRect.xMax + 4f,
                        rowRect.y,
                        rowRect.xMax - matchRect.xMax - removeButtonWidth - 6f,
                        rowHeight);

                    DrawConstrainedMatchType(matchRect, matchTypeProp, queryProp, i);
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
                    SerializedProperty newMatchType = newElement.FindPropertyRelative("matchType");
                    if (newMatchType != null)
                        newMatchType.enumValueIndex = 0; // default to first enum value
                }

                line.y += EditorGUIUtility.singleLineHeight +
                          EditorGUIUtility.standardVerticalSpacing;

                if (HasImpossibleRules(queryProp))
                {
                    Rect helpRect = line;
                    string msg = "This query contains rules that can never be satisfied (conflicting MatchTypes for overlapping items).";
                    float helpHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(msg), contentRect.width);
                    helpRect.height = helpHeight;
                    EditorGUI.HelpBox(
                        helpRect,
                        msg,
                        MessageType.Error);
                    line.y += helpHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                string summary = BuildSummaryText(queryProp);
                if (!string.IsNullOrEmpty(summary))
                {
                    Rect summaryRect = line;
                    float summaryHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(summary), contentRect.width);
                    summaryRect.height = summaryHeight;

                    EditorGUI.HelpBox(summaryRect, summary, MessageType.Info);
                    line.y += summaryHeight + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            EditorGUI.EndProperty();
        }

        private void DrawConstrainedMatchType(
            Rect position,
            SerializedProperty matchTypeProp,
            SerializedProperty queryProp,
            int elementIndex)
        {
            if (matchTypeProp == null)
            {
                EditorGUI.LabelField(position, "Invalid MatchType");
                return;
            }

            string[] allNames = matchTypeProp.enumDisplayNames;
            int enumCount = allNames.Length;

            List<int> validValues = new List<int>();
            List<string> validNames = new List<string>();

            for (int enumIndex = 0; enumIndex < enumCount; enumIndex++)
            {
                if (IsMatchTypeValid(queryProp, elementIndex, enumIndex))
                {
                    validValues.Add(enumIndex);
                    validNames.Add(allNames[enumIndex]);
                }
            }

            if (validValues.Count == 0 || validValues.Count == enumCount)
            {
                EditorGUI.PropertyField(position, matchTypeProp, GUIContent.none, true);
                return;
            }

            int currentEnumIndex = matchTypeProp.enumValueIndex;

            if (!validValues.Contains(currentEnumIndex))
            {
                validValues.Add(currentEnumIndex);
                validNames.Add(allNames[currentEnumIndex] + " (invalid)");
            }

            int newEnumIndex = EditorGUI.IntPopup(
                position,
                currentEnumIndex,
                validNames.ToArray(),
                validValues.ToArray());

            matchTypeProp.enumValueIndex = newEnumIndex;
        }

        private bool IsMatchTypeValid(SerializedProperty queryProp, int elementIndex, int candidateEnumIndex)
        {
            if (queryProp == null || queryProp.arraySize == 0)
                return true;

            // Build item set for the candidate element
            if (!TryGetElementAndItems(queryProp, elementIndex, out SerializedProperty candidateElement, out HashSet<(long, long)> candidateItems))
                return true;

            for (int i = 0; i < queryProp.arraySize; i++)
            {
                if (i == elementIndex)
                    continue;

                if (!TryGetElementAndItems(queryProp, i, out SerializedProperty otherElement, out HashSet<(long, long)> otherItems))
                    continue;

                if (!HasItemIntersection(candidateItems, otherItems))
                    continue;

                SerializedProperty otherMatchTypeProp = otherElement.FindPropertyRelative("matchType");
                if (otherMatchTypeProp == null)
                    continue;

                int otherEnumIndex = otherMatchTypeProp.enumValueIndex;

                if (IsCombinationImpossible(candidateEnumIndex, otherEnumIndex))
                    return false;
            }

            return true;
        }

        private bool HasImpossibleRules(SerializedProperty queryProp)
        {
            if (queryProp == null || queryProp.arraySize <= 1)
                return false;

            for (int i = 0; i < queryProp.arraySize; i++)
            {
                if (!TryGetElementAndItems(queryProp, i, out SerializedProperty elementA, out HashSet<(long, long)> itemsA))
                    continue;

                SerializedProperty matchTypeAProp = elementA.FindPropertyRelative("matchType");
                if (matchTypeAProp == null)
                    continue;
                int matchA = matchTypeAProp.enumValueIndex;

                for (int j = i + 1; j < queryProp.arraySize; j++)
                {
                    if (!TryGetElementAndItems(queryProp, j, out SerializedProperty elementB, out HashSet<(long, long)> itemsB))
                        continue;

                    if (!HasItemIntersection(itemsA, itemsB))
                        continue;

                    SerializedProperty matchTypeBProp = elementB.FindPropertyRelative("matchType");
                    if (matchTypeBProp == null)
                        continue;
                    int matchB = matchTypeBProp.enumValueIndex;

                    if (IsCombinationImpossible(matchA, matchB))
                        return true;
                }
            }

            return false;
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

                SerializedProperty matchTypeProp = element.FindPropertyRelative("matchType");
                if (matchTypeProp == null)
                    continue;

                int matchIndex = matchTypeProp.enumValueIndex;

                string joinedNames = "{" + string.Join(", ", itemNames) + "}";

                string ruleDescription = matchIndex switch
                {
                    0 => $"allows objects that contain at least one of {joinedNames}",
                    1 => $"requires objects to contain all of {joinedNames}",
                    2 => $"forbids objects that contain any of {joinedNames}",
                    3 => $"forbids objects that contain all of {joinedNames} together",
                    _ => null
                };

                if (!string.IsNullOrEmpty(ruleDescription))
                    parts.Add(ruleDescription);
            }

            if (parts.Count == 0)
                return string.Empty;

            return "This query: " + string.Join(" and ", parts) + ".";
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

            SerializedProperty pickerProp = element.FindPropertyRelative("picker");
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

                items.Add((a, b));
            }

            return true;
        }

        private List<string> GetItemNamesFromElement(SerializedProperty element)
        {
            List<string> result = new List<string>();
            if (element == null)
                return result;

            SerializedProperty pickerProp = element.FindPropertyRelative("picker");
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

        private static bool IsCombinationImpossible(int matchA, int matchB)
        {
            // 0 = Any, 1 = All (positive) | 2 = SomeNot, 3 = None (negative)
            // Invalid only when the same tag is in a positive and a negative rule.
            bool aPositive = matchA is 0 or 1;
            bool bPositive = matchB is 0 or 1;
            bool aNegative = matchA is 2 or 3;
            bool bNegative = matchB is 2 or 3;

            if ((aPositive && bNegative) || (aNegative && bPositive))
                return true;

            return false;
        }
    }
}

