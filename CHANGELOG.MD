# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

### Unreleased


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


