# Scriptable Object Collection — Project Instructions

## Project Overview

This is a **Unity Package** (`com.brunomikoski.scriptableobjectcollection`) that improves the usability of ScriptableObjects by grouping them into collections with code generation, custom inspectors, and GUID-based referencing. It targets **Unity 6000.0+** and is distributed via UPM (Unity Package Manager).

## Language and Runtime

- **C# 9** (Unity's default for Unity 6). Do not use C# 10+ features (e.g., `global using`, file-scoped namespaces, raw string literals).
- Target **.NET Standard 2.1** APIs only. Do not use APIs exclusive to .NET 5/6/7+.
- `unsafe` code is **not allowed** (`allowUnsafeCode: false` in assembly definitions).
- Use `is` and `is not` pattern matching, target-typed `new()`, and null-coalescing where appropriate — these are available in C# 9.

## Project Structure

```
Scripts/
  Runtime/       → Runtime assembly (BrunoMikoski.ScriptableObjectCollection)
  Editor/        → Editor assembly (BrunoMikoski.ScriptableObjectCollection.Editor)
    Browser/     → Editor Browser sub-assembly
    Core/        → Settings, code generation, dropdowns
    CustomEditors/ → Custom Inspector editors
    Processors/  → Asset post-processors
    PropertyDrawers/ → Custom property drawers
    Generators/  → Static code generators
Samples~/        → UPM samples (ignored by Unity unless imported)
Documentation~/  → UPM documentation (ignored by Unity at runtime)
```

- **Runtime code must never reference `UnityEditor`** unless wrapped in `#if UNITY_EDITOR` / `#endif`.
- Editor-only logic in runtime files must always be inside `#if UNITY_EDITOR` blocks.
- Assembly definitions (`.asmdef`) define compilation boundaries — do not add cross-assembly references without updating them.

## Code Style and Conventions

### Naming
- **Namespace**: `BrunoMikoski.ScriptableObjectCollections` (with `s`) for all types. Sub-namespaces: `.Picker`.
- **Private fields**: `camelCase`, no underscore prefix (e.g., `private LongGuid guid;`).
- **Properties**: `PascalCase` (e.g., `public LongGuid GUID => guid;`). Acronyms stay uppercase (`GUID`, `SOC`).
- **Constants**: `UPPER_SNAKE_CASE` for private string constants in editors (e.g., `ITEMS_PROPERTY_NAME`), `PascalCase` for public constants.
- **Classes**: One class per file. File name matches class name.
- **Interfaces**: Prefixed with `I` (e.g., `ISOCItem`).

### Formatting
- 4-space indentation (no tabs).
- Allman-style braces (opening brace on new line).
- UTF-8 encoding **without BOM** for all `.cs` files.
- Normalize line endings (handle both `\r\n` and `\n`).

### Patterns
- Prefer `for` loops over `foreach` in hot paths and runtime code to avoid enumerator allocations.
- Use `[SerializeField]` with `private` fields; avoid public serialized fields.
- Use `[HideInInspector]` for serialized fields that shouldn't appear in the default inspector.
- Use `[FormerlySerializedAs("oldName")]` when renaming serialized fields to preserve data.
- Avoid LINQ in runtime hot paths (allocations). LINQ is acceptable in editor code.
- Cache lookups in dictionaries when collections are iterated frequently (see `itemGuidToScriptableObject`, `itemNameToScriptableObject` patterns).
- Always validate caches: when returning a cached value, verify it's still valid (not destroyed, GUID still matches) before trusting it. Evict stale entries.

### Unity-Specific
- Use `ObjectUtility.SetDirty(obj)` instead of calling `EditorUtility.SetDirty` directly (it wraps the editor check).
- Use `ScriptableObject.CreateInstance<T>()` to instantiate ScriptableObjects, never `new`.
- Null-check Unity objects carefully: use `.IsNull()` extension or explicit `== null` (Unity overrides `==` for destroyed objects). Standard `is null` does **not** detect destroyed Unity objects.
- Use `AssetDatabase` APIs only inside `#if UNITY_EDITOR` blocks or in Editor assemblies.
- Use `TypeCache` in editor code for efficient type lookups instead of reflection-heavy alternatives.

## GUID System

- Items and collections use `LongGuid` (a 128-bit struct, two `long` values) — not Unity's `string` GUIDs.
- GUID validation and uniqueness is enforced by `SOCItemGuidProcessor` (an asset postprocessor), not at item creation time.
- When working with GUIDs: always check `guid.IsValid()` before using. Generate new GUIDs with `GenerateNewGUID()`.

## Code Generation

- Generated scripts use UTF-8 without BOM.
- Line endings are normalized before writing.
- Generated files use `partial class` and the `new` modifier where appropriate to avoid compiler warnings.

## PR and Review Guidelines

- Keep runtime and editor changes clearly separated.
- Any new serialized field rename must include `[FormerlySerializedAs]` for backward compatibility.
- New editor UI should use UXML/UIToolkit where the existing editors do, but IMGUI is acceptable for property drawers.
- Avoid introducing GC allocations in runtime `Matches()` or lookup methods — reuse collections (see `HashSet` reuse pattern in `CollectionItemQuery`).
- Conditional compilation (`#if UNITY_EDITOR`, `#if ADDRESSABLES_ENABLED`) must be used correctly; do not assume optional packages are present.
