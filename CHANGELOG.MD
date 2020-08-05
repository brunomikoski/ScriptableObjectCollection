# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## [1.1.9]
## Changed
 - Fixed AOT compilation issue
 
## [1.1.8]
## Changed
 - Fixed mobile builds (using editor references on runtime files)

## [1.1.7]
## Added
 - Proper error when there's no static script folder defined
 - Default namespace on settings
 - Stored the last scripts folder as cache for the wizzard
 - Validation of null items on the Registry
 
## Changed
 - Changed to the static file use the Colllectable Type again
 - Values on the static files now its just values not `CollectableType+Values`

## [1.1.6]
## Changed
 - Crash while changing to debug mode while having the Collection inspector open
 - Fix static generation code writing the full class name as the static class name

## [1.1.5]
## Changed
 - Fix possible stackoverflow on PostProcessor
 - Removed Assembly Definition Check (Was expensive and bad when working with packages)
 - Fixed issue when reloading items from the same type from different collections been added to both
 - Fixed Static genearation file using sanitized name and not class name, breaking the partial class
 - Fixed Create Collection context menu using the selected folder as target for both scrips and scriptable objects
 - Fixed some warnings
 - Fixed weird pair next name issue
  
 ## Added
 - You can now set specific overwrite static files location on the `CollectionSettings` file
 - Settings are now displayed on the Collection itself rather than on the registry

## [1.1.4]
## Changed
 - Fixed issue with package version

## [1.1.3]
## Added
 - Added support for codacy code quality
## Changed
 - Refactored settings 
 - Removed settings from the Collection script to make it clear
 - Added some a way to write inside packages as well.
 
## [1.1.2]
### Changed
 - Changed the Settings file to be the main place where data is stored, not using EditorPrefs anymore
 - Added all the namespaces for individual assets as diretives on top of static generated file

## Fixed
- Fixed issue with the Add New Item missing the DelayCall on the Collection Editor Window

## [1.1.1]
### Added
- Added the `[CreateAssetMenu]` to the Collection automatic generated file

### Changed
- The Static file uses the Collection name instead of the collectable type, so you can have multiple collections of the same type and unique access to that specific collection
- Fixed asset creation menu name to use `ScriptableObject` instead of `Scriptable Object`

## Fixed
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

## [Unreleased]
 - Add a setup wizzard for first time settings creation

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


