# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## [1.3.2]
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

### [Unreleased]

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


