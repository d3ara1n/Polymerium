## [Unreleased]

### ✨ Highlights ✨

- Add curated color palettes that apply a matching gray scale and accent color pair with one click
- Add support for declaring a range of compatible Java versions in native patches

### Fixed

- Fix instances imported by an earlier version failing to deploy after Java compatibility became a declared range
- Fix native patch customizations being lost during migration or applied inconsistently during deployment

### Added

- Add curated color palettes that apply a matching gray scale and accent color pair with one click (Huskui.Avalonia)
- Add a gray scale option to the appearance settings (Huskui.Avalonia)
- Add support for declaring a range of compatible Java versions in native patches
- Add a prompt pointing to the Java settings when an instance has no compatible Java runtime

### Changed

- Change the default appearance to the Ember palette with a warm gray scale and an amber accent (Huskui.Avalonia)
- Change Java selection to use the newest compatible Java version you have configured and fall back to the bundled runtime otherwise

### Removed

- Remove the up-front check of a configured Java path and report a wrong path when launching instead
