# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.3.0]
### Changed
- Refactored CollectionCustomEditor to use UI Elements, getting rid of multiple hacks to make it work with the IMGUI and Reorderable list
- Updated the visuals of the Collection Editor
- Advanced Options are now exposed by default, giving more clarify of what is going on
- Removed the editor only assets from the Collection (`generateAsPartialClass`, `generateAsBaseClass`, `generateAsStaticClass`), those are not stored on the SOCSettings file and versioned
- The `guid`, `items` and `automaticallyLoaded` are now `HideInInspector` on the Collection, to avoid confusion
- Added new USS and UXML files to the Collection Editor
- Generated static files now contains the default `.g.cs` postfix 


### Added
- Added new Default Inspector session to the Collection Editor, where you can see the default inspector of the collection and expose more items there.


### Know issues
- Settings will not be automatically converted to the new SOCSettings file, settings like _(custom namespace, filename, or target folder)_ so you have to reconfigure those settings manually

## [2.2.4]
### Added
- Added Item Order Protection. Override the `ShouldProtectItemOrder` property in your Collection to disallow items being dragged or deleted.

### Fixed
- Fixed SOC Item property drawers not working in certain Unity versions by @RoyTheunissen in https://github.com/brunomikoski/ScriptableObjectCollection/pull/141

## [2.2.3]
### Fixed
- `[SOCItemEditorOptions]` now has the option to limit the displayed items to the collection selected in a given field using the ConstrainToCollectionField parameter.
- `[SOCItemEditorOptions]` now has the option to fire a callback when a value is selected using the OnSelectCallbackMethod parameter.
- Replaced the implementation of the `[DidReloadScripts]` by the `AssetPostprocessor` to deal with importing errors on newer unity version
- Fixed issue where trying to add new custom items to a collection was not showing the create custom type properly

## [2.2.2]
### Fixed
- Fixed Collection Picker wrong intiialization on a cold registry
- Fixing Collection Picker not respecting children rules
- Fix drawing go to button on null items.

## [2.2.1]
### Fixed
- Update active editor when selecting a new item with the keyboard


## [2.2.0]
### Added
- Scriptable Object Browser window


## [2.1.3]
### Fixed
- Fixed scripts folder validation warning showing incorrectly when mirroring folder
- Fixed namespaces ending with dot


## [2.1.0]
### Added
- Introducing Generators (thanks @RoyTheunissen). The overall idea is to make it simple to generate Collection Items based on other rules, for instance Unity Scenes, Unity Tags, Layers and anything else you want,check the help wiki about how to use it:  https://github.com/brunomikoski/ScriptableObjectCollection/wiki/Generators

### Changed
- Ability to define if a class can be reordered/add/remove items by the collection
- A few other helpers for creating SOCItems dynamically 


## [2.0.9]
### Changed
- Editor serialization issues with new drawing methods.
- Added sorting options for collections
- Added option to make the collection not reorderable
- Fixed issue with post processing and recent imported files

## [2.0.8]
### Changed
- Fixed a small typo in the package description.
- Since [2.0.3] the collection now uses the Editor for drawing the items on the collection, but this can cause some issues depending of how customized it is inside a ReorderableList, so you now can choose the item to use it or not by toggling the `Use Custom Editor` inside the advanced settings
- Upgraded the `CollectionCustomEditor` to use `BaseEditor`
- Updated `CollectionItemPicker<>` to use `IndirectReference<>` to store the items, since also contains the `LongGuid` reference to the Collection, allowing to work with multiple collections of the same time _(It should automatically upgrade to the new version automatically)_
- Added ability to compare `CollectionItemPicker<>` to a `IList<>` of the same type
- Other small fixes and improvements


## [2.0.7]
### Changed
- Moved `EditorPreference` to Runtime folder. It is safe and useful to be able to use it in runtime code.

## [2.0.6]
### Changed
- Added `IsValid()` method for indirect references
- Added more checks and logs for when regenerating guids and cleaning bad items from collection
- Fixed issue with `CollectionItemPicker` not respecting base type as the starting type
- Removed the `Application.isPlaying` check for when setting items dirt

### Removed
- Unused `ScriptableObjectForPreferences`

## [2.0.5]
### Changed
- Reverted the change where the `isExpanded` property would be shared between the collection editor and the property drawer.
- Renamed `ObjectType` to `TObjectType` to keep standardized
- Added `TryGetItemByName` for the `ScriptableObjectCollection.cs`
- Added extra safety methods for checking duplicated `Guids` on collections and collection items
- Properly implemented `CollectionItemPicker` `IEnumerator<TItemType> GetEnumerator()`

## [2.0.4]
### Changed
- Added boolean check for static generated files to improve performance
- Changed settings to be stored on ProjectSettings folder and be shared by subversion
- Fixed `CollectionItemPickerPropertyDrawer` to support multiple collections of the same type
- Other small optimizations and code formatting

## [2.0.3]
### Changed
 - Collection now uses the appropriated Editor for each CollectionItem when displayed on the Collection
 - Fixed expanded state not being saved on the Collection

## [2.0.2]
### Changed
 - Fixed editor only icons causing issue when the project recompiles during playmode.
 - Added option to disable automatically reloading of collection on the CollectionRegistry
 - Updated Labels from `CollectionItemPicker` to use black as the default interface
 - Added some helpers to help project upgrade
 - Added `[DrawAsSOCItem]` attribute to allow drawing of custom `ScriptableObjec` that implements `ISOCItem` as a CollectionItem, this is only necessary for unity versions bellow 2022, should not affect anything else.
 - Syncronize Assets button on the Collection will also perform a reload of the collection

## [2.0.1]
### Changed
- Refactored `CollectionItemPicker` to only use `LongGUID` and also the PropertyDrawer has been remade, now looks nicer 🤩
- Added multiple helpers to `CollectionItemPicker` to be used more like a label system/
- Added `ISOCColorizedItem` to allow coloring of the `CollectionItemPicker` on the inspector, I will expand this to other areas as well. 

## [2.0.0]
### Changed
- Fixed issue with name parser that would still treat digits as string and added to the final name
- Fixed issue with PropertyDrawer of indirect reference without attribute would cause a null reference
- Refactored the system to use ScriptableObject as a base class instead of `ScriptableObjectCollectionItem` now any `ScriptableObject` can be used as a collection item as long as implements the `ISCOItem` interface
- Fixed issue with `CollectionItemPicker` that would not work properly with `ScriptableObject` that are not in the same assembly as the `ScriptableObjectCollection`
- Added new buttons on CollectionRegistry to force guid validation on all items
- Reduced automatic collection guid reloading to reduce performance impact on larger projects
- Fixed issues with target scripts generation folder not being saved properly
- Minimum supported Unity version is now 2021 _(I'm DONE with 2020, its the worst unity ever 😒)_


### Removed
 - Removed support for Addressables, it was not working properly and it was not being used by a lot of teams, I think the addressables support should be handled elsewhere.

### Breaking Changes
- If you used the Addressables integration `SOC_ADDRESSABLES_SUPPORT` be extra careful when upgrading
- The old string based guid system has been replaced for a lighter version `LongGuid`
- If you are using IndirectReferences all the references will be lost, you will need to re-set them

### Upgrade Guide from 1.x.x to 2.x.x
- Update the package
- Make sure you click on the `CollectionRegistry` and click on the `Validate Collections` button
- Regenerate all your Static Code Files


# [1.9.7]
### Added
- Initial string search support

### Changed
- Fixed issue with namespace not been updated on first usage

# [1.9.6]
### Added
- Implemented the new `CollectionItemPicker<ItemType>` that act as a `[Flag]` enum, on the inspector and is a List under the hood. Thanks [@RoyTheunissen](https://github.com/RoyTheunissen) for the contribution

### Fixed
- Issue where the type was not properly loaded when used as a `IList`

# [1.9.5]
### Changed
- Fixed Compiler Warning
- Fixed another nullreference on `GetCollectionItemHeight`
- Fixed `NullReference` on `CheckForKeyboardShortcuts()`
- Fixed reserved names C# names using `IgnoreCase` for comparison, now you can use reserved name as UpperCase
- Now static generated classes will use the `cached` naming prefix, to avoid issues with reserved C# variables names
- Fixed remaining Warning.

# [1.9.4]
### Changed
- Fixed namespace settings not been stored properly after changing once on the Wizard
- Fixed `NullReference` on GetItemHeight()

# [1.9.3]
### Add
- Automatically define namespace for static collection generated file based on `CollectionItem`

### Changed
- Fixed static static file generation to use `Type.FullName` instead of `Type.Name`

# [1.9.2]
### Changed
- Fixed compatibility issues with Unity 2021
- Fixed Wizard settings been shared between multiple projects

# [1.9.1]
### Changed 
- Fixed non editor builds issue 

# [1.9.0]
### Added 
- Added new Collection References, useful for individual collection loading, more information [here](https://github.com/brunomikoski/ScriptableObjectCollection/pull/99) thanks a lot [@IkeikeP](https://github.com/IkeikeP) for the contribution

# [1.8.3]
### Changed
- Fixed `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]` issue with unity 2021 and generics.

# [1.8.2]
### Changed
- Fix issue where entering in playmode with a missing collection reference in the CollectionRegistry would throw a nullref and not reload collections
- Added new condition to consider an item as a "Bad Item" and for it to be cleared. This fixed an edge case where a collection can have a non existing item but it is not being cleared out from the collection
- Added validation method as an `CollectionItemEditorOptions` option, you can now define a method to validate what CollectionItem can appear, like this:
```csharp
[SerializeField, CollectionItemEditorOptions(ValidateMethod = nameof(IsValidConsumable))]
private Consumable onlyUnlockedHardCurrency;

public bool IsValidConsumable(Consumable consumable)
{
    if (consumable is HardCurrency hardCurrency)
        return hardCurrency.IsUnlocked;
    return true;
}
```

# [1.8.1]
### Changed
- Added `[Preseve]`attribute to both `ResourceScriptableObjectSingleton` and `CollectionsRegistry` to avoid AOT errors
- Added check if item already exist in the collection before adding it 

## [1.8.0]
### Added
- Renamed uses of `Wizzard` to `Wizard`
- Wizard foldouts now work
- The collection name is now based on the item name by default. You can still override it.
- The script folder now mirrors the Scriptable Object folder by default
- The namespace is now inferred from the script folder by default, with options for how it should be formatted. Can still manually override.
- Improved wizard readability by tweaking the layout, adding a help text, adding a scrollbar and defining a reasonable minimum size
- Settings that are serialized using Editor Prefs now use the new EditorPreference utility class
### Changed
- Renamed uses of `Wizzard` to `Wizard`
- Wizard now focuses on Item Name by default so you can type immediately
- Reformatted wizard to look more like a Unity built-in feature
- Parent folders are no longer created by default in preference to mirroring the ScriptableObject hierarchy
### Deprecated
- ScriptableObjectCollectionUtils is no longer responsible for appending any parent directories. If you were using any of those methods, please use the recommended method instead.
- ScriptableObjectCollectionSettings.SetDefaultNamespace has been renamed. Please use SetNamespacePrefix instead.

## [1.7.0]
### Changed
 - **BREAKING CHANGE** Removed Obsolete methods related to Previous Naming, if you are still on version bellow [1.5.4] consider implementing the suggested warnings before upgrading
 - `ScriptableObjectCollection` its now abstract to avoid issues mentioned here [here](https://github.com/brunomikoski/ScriptableObjectCollection/issues/85#issue-1009507869) 
 - Fixed partial toggle been disabled when non target generated folder is set
 - Fixed issue when its not possible to read the current `ScriptableObjectItem`  assembly [@RoyTheunissen](https://github.com/RoyTheunissen)
 - Fixed a bunch of grammar mistakes on the Readme file [@RoyTheunissen](https://github.com/RoyTheunissen)
 - Added more information to the faq [@RoyTheunissen](https://github.com/RoyTheunissen)
 - Replaced TypeUtility by Unity [TypeCache](https://docs.unity3d.com/ScriptReference/TypeCache.html)
 - Fixed issue when a broken script between a CollectionItem is missing would cause null references on the inspector

## [1.6.5]
### Added 
- More display options (Hide Preview button / Hide GoTo Button) @FreezyExp
### Changed
- Fixed null reference on the GoTo button on the indirect references. @FreezyExp
- Fixed warning from ResourceLoading @FreezyExp
- Fixed issue where CollectionRegistry would be stripped depending on the stripping level @FreezyExp
- More small improvements and changes (https://github.com/brunomikoski/ScriptableObjectCollection/pull/83

## [1.6.4]
### Changed
- Changed the `ResourceScriptableObjectSingleton` to have generic type on methods again, to prevent AOT issues.

## [1.6.3]
### Added
- Added Delete Collection button under advanced settings, I will delete all the related assets and unregister from the registry
### Changed
- Renamed the `Settings` menu on the Collection Editor to `Advanced`
- Fixed an issue when drawing indirect references inside arrays.

## [1.6.2]
### Added
- Added `generateAsBaseClass` as an option for each collection to allow the access each Collection Item as base type.

## [1.6.1]
### Added
- Added arrow navigation on the Collection Editor, now you can navigate between items and expand/collapse it by <kbd>left</kbd> <kbd>right</kbd>
- You can now expand/collapse all items by holding <kbd>alt</kbd> while clicking on the foldout arrow, like default unity controls

## [1.6.0]
### Changed
- Refactored the `ScriptableObjectCollection` `Custom Editor` to use `ReorderableList` instead of the previous custom list drawer
- Refactored the editor prefab property drawer to properly draw the `SerializedProperties` of the item instead of displaying the custom inspector
- Removed some old files and methods that aren't necessary anymore.
- When using the inline visualizer of the CollectionItem, it should now respect position (if is inside an array or custom propery drawer)
### Added
- Added context click on the items, you can now <kbd>Copy</kbd> <kbd>Paste</kbd> values between collection items and <kbd>Duplicate</kbd> and <kbd>Delete</kbd> items when right click on it on the Editor

## [1.5.7]
### Changed
- Removed the asset destruction of the Collection on the `OnDestroy` event

## [1.5.6]
### Changed
- Improved the Refreshing of the `CollectionRegistry` to try to refresh when items are deleted/created
- Refactored the `IndirectReference` to not need a serialized version of the `CollectionItem` type, so avoid any serialization issues
- Renamed some of the editor classes to match the standard

## [1.5.5]
### Changed
- In order to support multiple collections of the same type the check of the `Values` I had to tweak how the instance of every instance its assigned, changed for the `OnEnable` of the collection item
- Changed the Refresh of the collections to only find items under the same folder.
- Fixed a lot of scripts to work properly with multiple collections of the same type
- Added some logs when `CollectionItems` are added/removed from collections to help the visibility
- `CollectionType.Values` now will return all the `CollectionItem` available on the registry, so if you have 2 Collections of the one Type this will return the content of both collections while the `CollectionItem.Values` when the access file is generated will only return that specific collection items

## [1.5.4]
### Changed
- Removed the automatically reload of CollectionRegistry using the `DidReloadScripts` callback, this caused multiple issues on batch mode and on clear libraries
- Converted the `Collection` reference on the `CollectionItem` to be a Lazy reference, since this was causing some dependency when using `CollectionItem` as addressables
- Refactored the `ScriptableObjectSettings` this used to be a Resources object that was auto loaded, but this could also create unwanted dependencies between items. The settings for code generated are not stored inside the Collection itself. Make sure you delete the `ScriptableObjectsSettings` ScriptableObject inside your resources folder.
- Added a bunch of validation and verification for the Generation of the static access, making sure it only allows partial classes when is possible and other small checks.
- The settings are now on the Project Preferences where you can define the default folder for the Scriptable Objects and default namespace.
- Removed the `ReadOnlyList` on the `Collection`, the casting was expensive and was an unnecessary safety measure

## [1.5.3]
### Changed
- Disabled the reload of the collection after script reloading on batch mode. 

## [1.5.2]
### Changed
- Deleted old .meta files

## [1.5.1]
### Changed
- Simplified the multiple collection display to be a single line again, showing all the available items inside the `AdvancedDropdown`

## [1.5.0]
### Changed
- Refactored the `Collectable` for `CollectionItem` this has several changes in multiple parts of the code, I tried my best to keep compability with the old version as well.
- Renamed the `CollectableScriptableObject` to `ScriptableObjectCollectionItem` for better naming convention, `CollectableScriptableObject` still exist but has the obsolete flag on it to avoid breakable changes
- Add `IsValid` to IndirectReferences to check if has valid data before being used
- Refactored `ResourceScriptableObjectSingleton` to fix some warnings
- Fixed the `GetEnumerator` from `Collection` warning
- The `CollectionRegistry` now will only load Collections that are inside an active Assembly.
- Exposed the `Collections` list on the Registry
- Simplified the CollectionItem Dropdown (removed the type grouping)
### Added
- A new information on the Collection custom editor to show the base class if is different from the collection type (Disabled for now)
- Exposed the namespace to be customized by the custom static file
- Added support of multiple collections of the same type on the registry, the `CollectionItemDrawer` will display a Collection dropdown if more than one collection for the same type of item is available

## [1.4.1]
### Changed
- Fixed issue when using the Create Settings menu
- Added automatically open of the selected collectable when using the goto button on the CollectableProperty Drawer
- Added type specific GetEnumerator for the Collection

## [1.4.0]
### Added
- Added quick access to the .Values from the collection, to be used without code generation, you can now use `CustomCollection.Values` to gett all your items
- Added a `GetValues()` to the generated static file, you can quickly get a filtered list of items that you need
### Changed
- Changed Create Collection Wizzard to behave like a modal window that can loose focus.
- Removed the TryGet static access generation, was not been used and was causing some problems


## [1.3.2]
### Added
 - Automatically selection of the newly created item, for renaming
 - Added keyword validation for names
 - Added ability to create new items from the selection dropdown.
### Changed
 - Fixed issue when trying to play without the `CollectionRegistry` created 

## [1.3.1]
### Added
 - Added basic copy/paste functionality between colelctables
### Changed
 - Removed multiple `AssetDatabase.SaveAssets();` to improve general performance when adding / removing objects
 - Fixed indirect reference issues with the new Advanced Dropdown search
 - Removed static `.Values` from the Colletion, was redundant and error prone

## [1.3.0]
### Added
 - Advanced dropdown for better searching on big collections;

## [1.2.9]
### Changed
 - Fixed issue with the Foldout issue, now showing multiple inherited collectables should work fine
 - Improved the ADD New on the collection and what is displayed, and the the creation of new types is on a subfolder.
 - Added static access to the `ScriptableObjectCollection<>.Values`
 - Renamed the generated Static value back to the `.Values` to keep in line with the static access. so you can now access by `Collection.Values` or `Collectable.Values`
### Added
 - Support to create Collections / Collectables based on a Utils script, useful for another packages creating the necessary Collection.
 
## [1.2.8]
### Changed
 - Fixed issue when trying to create a new type from abstract using the new Collectable editor.
 
## [1.2.7]
### Changed
 - Fixed issue when duplicating assets caused a crash
 - Fixed issue when deleting Scripts from base Collectables
 - Fixed issue of null type when creating a new collectable with base type 
### Added
 - Added new inspector to create new base types from the Editor.

## [1.2.6]
### Changed
 - Fixed AOT issue.

## [1.2.5]
### Changed
 - Fixed issue with the read only lists not refreshing properly on Inspector calls on PlayMode.

## [1.2.4]
### Changed
 - Fixed issue where the collection view was not properly refreshing
 - Fixed general warnings on the project
 - Fixed double type usage on the ResourceScriptableObject
 - Renamed the `Values` to `Collection` on the static generated file, it makes more sense to the new Custom Static Class access
### Added
 - Added a new Custom Static File to be able to generate static access files between different assemblies
 - Added warning system to help displaying issues
 
## [1.2.3]
### Changed
 - Not serializing editor fields
 - Fix read only list not storing caching

## [1.2.2]
### Changed
 - Fixed issue with the settings menu been displayed wrong
 - Fixed an issue while converting numbers to literal numbers would not deal properly with initial special characters
### Added
 - Added system to be able to Add/Remove items from the collection at runtime (Like loading new items from addressables)
 - Added runtime / editor time fix to deal with dynamicly changed collections

## [1.2.1]
### Changed
 - Fixed issues with indirect references

## [1.2.0]
### Changed
 - Fixed issue with the wizzard that was not generating the target Scriptable Object
### Added 
 - New type of indirect access to collectable items, this allows you use the regular editor reference, but without storing the reference, so without creating a relationship between the items, and can be lazy loaded when is needed, like addressables references for isntance.
 - Also added an option on the wizzard to automatically create the IndirectReference for every collectable item

## [1.1.9]
### Changed
 - Fixed AOT compilation issue
 
## [1.1.8]
### Changed
 - Fixed mobile builds (using editor references on runtime files)

## [1.1.7]
### Added
 - Proper error when there's no static script folder defined
 - Default namespace on settings
 - Stored the last scripts folder as cache for the wizzard
 - Validation of null items on the Registry
### Changed
 - Changed to the static file use the Colllectable Type again
 - Values on the static files now its just values not `CollectableType+Values`

## [1.1.6]
### Changed
 - Crash while changing to debug mode while having the Collection inspector open
 - Fix static generation code writing the full class name as the static class name

## [1.1.5]
### Changed
 - Fix possible stackoverflow on PostProcessor
 - Removed Assembly Definition Check (Was expensive and bad when working with packages)
 - Fixed issue when reloading items from the same type from different collections been added to both
 - Fixed Static genearation file using sanitized name and not class name, breaking the partial class
 - Fixed Create Collection context menu using the selected folder as target for both scrips and scriptable objects
 - Fixed some warnings
 - Fixed weird pair next name issue
### Added
 - You can now set specific overwrite static files location on the `CollectionSettings` file
 - Settings are now displayed on the Collection itself rather than on the registry

## [1.1.4]
### Changed
 - Fixed issue with package version

## [1.1.3]
### Added
 - Added support for codacy code quality
### Changed
 - Refactored settings 
 - Removed settings from the Collection script to make it clear
 - Added some a way to write inside packages as well.
 
## [1.1.2]
### Changed
 - Changed the Settings file to be the main place where data is stored, not using EditorPrefs anymore
 - Added all the namespaces for individual assets as diretives on top of static generated file
 - Fixed issue with the Add New Item missing the DelayCall on the Collection Editor Window

## [1.1.1]
### Added
- Added the `[CreateAssetMenu]` to the Collection automatic generated file
### Changed
 - The Static file uses the Collection name instead of the collectable type, so you can have multiple collections of the same type and unique access to that specific collection 
 - Fixed asset creation menu name to use `ScriptableObject` instead of `Scriptable Object`
 - Editor Group issues when adding new items throwing error

## [1.1.0]
### Added 
 - Added support for non automatically loaded Collections, this allow you initialize collections when they are necessary, avoiding adding all the collectable 
 references into the resources bundle
 - Added `InitializeCollections` that can initialize Collection in regular MonoBehaviours
 - New type of script static script generation for Non automatically loaded collections
 - Added new Custom Editor for the registry where you can define witch collection are auto loaded, and the static generation type
 - PreProcess that removes the non automatically loaded collections before build
### Changed
 - General bug fixes and optimizations
 - Fixed issue with the dropdown on property drawer
 
## [1.0.1]
### Added 
 - Support for SubClasses support for adding new items 
 - Asset Modification Processor to prevent issue when deleting `Collectable` and `Collections`
    - If you delete a `Collection` now, all the `Collectables` will be deleted as well and removed from the registry 

## [1.0.0]
### Added 
 - First initial working version 

[2.3.0]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v2.3.0
[2.2.4]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v2.2.4
[2.2.3]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v2.2.3
[2.2.2]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v2.2.2
[2.2.1]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v2.2.1
[2.2.0]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v2.2.0
[2.1.3]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v2.1.3
[2.1.0]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v2.1.0
[2.0.9]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v2.0.9
[2.0.8]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v2.0.8
[2.0.7]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v2.0.7
[2.0.6]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v2.0.6
[2.0.5]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v2.0.5
[2.0.4]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v2.0.4
[2.0.3]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v2.0.3
[2.0.2]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v2.0.2
[2.0.1]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v2.0.1
[2.0.0]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v2.0.0
[1.9.7]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.9.7
[1.9.6]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.9.6
[1.9.5]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.9.5
[1.9.4]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.9.4
[1.9.3]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.9.3
[1.9.2]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.9.2
[1.9.1]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.9.1
[1.9.0]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.9.0
[1.8.3]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.8.3
[1.8.2]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.8.2
[1.8.1]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.8.1
[1.8.0]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.8.0
[1.7.0]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.7.0
[1.6.5]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.6.5
[1.6.4]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.6.4
[1.6.3]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.6.3
[1.6.2]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.6.2
[1.6.1]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.6.1
[1.6.0]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.6.0
[1.5.7]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.5.7
[1.5.6]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.5.6
[1.5.5]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.5.5
[1.5.4]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.5.4
[1.5.3]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.5.3
[1.5.2]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.5.2
[1.5.1]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.5.1
[1.5.0]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.5.0
[1.4.1]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.4.1
[1.4.0]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.4.0
[1.3.2]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.3.2
[1.3.1]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.3.1
[1.3.0]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.3.0
[1.2.9]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.2.9
[1.2.8]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.2.8
[1.2.7]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.2.7
[1.2.6]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.2.6
[1.2.5]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.2.5
[1.2.4]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.2.4
[1.2.3]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.2.3
[1.2.2]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.2.2
[1.2.1]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.2.1
[1.2.0]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.2.0
[1.1.9]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.1.9
[1.1.8]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.1.8
[1.1.7]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.1.7
[1.1.6]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.1.6
[1.1.5]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.1.5
[1.1.4]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.1.4
[1.1.3]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.1.3
[1.1.2]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.1.2
[1.1.1]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.1.1
[1.1.0]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.1.0
[1.0.1]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.0.1
[1.0.0]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.0.0
