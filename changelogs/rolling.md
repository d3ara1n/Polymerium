## [Unreleased]

### ✨ Highlights ✨

- Introduce live language switching that applies the selected language immediately without restart (Irihi.Lingua)
- Add a toggle in onboarding and settings to opt out of sending anonymous crash and error reports

### Fixed

- Fix the code font setting not applying to code blocks in descriptions and release notes
- Fix restore leaving orphaned run-directory copies of pack files added after the snapshot was taken
- Fix the onboarding language choice requiring an app restart to take effect
- Fix snapshot restore failing on case-sensitive filesystems when files differing only by name casing coexist

### Added

- Introduce live language switching that applies the selected language immediately without restart (Irihi.Lingua)
- Add Minecraft color and style formatting to mod, resource pack, data pack, and server descriptions on the instance
  files page
- Add localized labels for the Minecraft version types in the new instance version picker
- Add a compatibility view to the project detail modal listing supported mod loaders and Minecraft versions (#POLY-142)
- Add a toggle in onboarding and settings to opt out of sending anonymous crash and error reports (#79)

### Changed

- Replace the one-time startup update notification with a persistent banner on the home screen when the auto-check finds a new version

### Removed

-
