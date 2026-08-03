# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/), 
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2023-07-12
### Fixed
- Fixed an issue causing the gun controller to spawn two projectiles when it should be spawning just one.
- Fixed an issue causing the projectiles to fail to clean themselves up after interacting with a smoke instance.

## [1.1.0] - 2023-06-15
### Added
- Added properties for the smoke albedo, main light tint, and ambient color tint
- Added properties for the shadow step count and step size
- Added an option to enable the smoke while in edit mode to make it easier to tweak and configure the smoke options
- Added an option to "Regenerate" the smoke while in edit mode to make it easier to tweak and configure the smoke options
- Added Startup Options. These options enable you to configure what an object does when it is spawned. You can choose to initiate on enable or wait for a scripting API action.
- Added Cleanup Options. These options enable you to configure what an object does when it is done. You can choose to destroy, disable, or wait for scripting API action.

### Changed
- Renamed some properties to make their usage more clear (e.g., "Expire Duration" -> "Fade Out Duration")

## [1.0.1] - 2023-05-30

### Fixed
- Fixed an issue where the asset was requesting a shader that did not exist
- Fixed a tooltip for the Interactive Explosion object

### Changed
- Changed the material editor so that it hides the unused properties

## [1.0.0] - 2023-04-13
- First release