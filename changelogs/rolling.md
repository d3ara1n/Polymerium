## [Unreleased]

### ✨ Highlights ✨

- Add support for declaring a range of compatible Java versions in native patches

### Fixed

- Fix instances imported by an earlier version failing to deploy after Java compatibility became a declared range

### Added

- Add support for declaring a range of compatible Java versions in native patches
- Add a prompt pointing to the Java settings when an instance has no compatible Java runtime

### Changed

- Change Java selection to use the newest compatible Java version you have configured and fall back to the bundled runtime otherwise

### Removed

- Remove the up-front check of a configured Java path and report a wrong path when launching instead
