# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[1.0.0]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.0.0
[1.0.1]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.0.1
[1.0.1]: https://github.com/badawe/ScriptableObjectCollection/releases/tag/v1.1.0


