## [Unreleased]

### ✨ Highlights ✨

- Introduce bulk selection to enable, disable or remove multiple packages at once in an instance's package list
- Introduce support for importing modpacks from packwiz repositories
- Fix the instance page sidebar expand state being reset after every launcher update

### Fixed

- Fix the instance page sidebar expand state being reset after every launcher update (Huskui.Avalonia.Mvvm)
- Fix a startup crash triggered when a second instance is launched while the first is still starting up (#POLYMERIUM-28)

### Added

- Introduce bulk selection to enable, disable or remove multiple packages at once in an instance's package list (#POLY-134)
- Introduce support for importing modpacks from packwiz repositories (#POLY-125)

### Changed

- Improve scroll performance of the Workspace diff view (Huskui.Avalonia.Code)
- Rework frosted glass backgrounds to render on the GPU, falling back to a solid panel color on systems without one

### Removed

-
