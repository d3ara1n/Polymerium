## [Unreleased]

### ✨ Highlights ✨

- Introduce collections to group packages in an instance's package list under a chosen name without referencing an external list

### Fixed

- Fix the AI analysis export uploading to mclo.gs before the save dialog and leaking access tokens and account paths in both the AI analysis and diagnostic packages (#82)
- Fix disbanding a group being recorded as a package change
- Fix crafted modpack archives placing files outside the instance directory during import (#POLY-151)
- Fix arbitrary program execution through command wrapper or Java home overrides in imported Trident packs (#POLY-152)
- Fix deployment rule destinations creating symbolic links outside the instance directory (#POLY-150)

### Added

- Introduce collections to group packages in an instance's package list under a chosen name without referencing an external list
- Introduce promoting a collection to a recipe and demoting a recipe group to a collection from the package group menu
- Add package-level actions to move a package in, out of, and between collections
- Add an action to rename a collection from its group header menu

### Changed

- Adjust package groups to collapse by default

### Removed

-
