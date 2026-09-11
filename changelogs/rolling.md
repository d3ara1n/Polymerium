## [Unreleased]

### ✨ Highlights ✨

- Add native patch support for customized instance launch environments
- Add support for running GTNH and Cleanroom instances

### Fixed

- Fix the update check button on the settings page staying disabled after the automatic check on startup completed
- Fix a configured Java path being silently replaced by another runtime instead of reporting that Java could not be found
- Fix removed native libraries lingering in the JVM native search directory
-

### Added

- Add native patch support for customized instance launch environments (#POLY-165)
- Add support for running GTNH and Cleanroom instances (#92)

### Changed

-

### Removed

- Remove the built-in MirrorChyan CDK and make GitHub the default update source (#POLY-144)
