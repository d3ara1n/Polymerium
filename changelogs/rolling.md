## [Unreleased]

### ✨ Highlights ✨

- Fix a failed modpack update partially deleting or corrupting the instance
- Reduce the installed size by over 40% through release-only trimming that keeps unannotated third-party assemblies intact
- Introduce persistent skip and hold update policies and changelog previews to the bulk update review modal

### Fixed

- Fix a failed modpack update partially deleting or corrupting the instance (#83, #POLY-153)
- Fix updating an instance that has never been launched (#POLY-153)
- Fix restoring a deleted snapshot wiping the instance's managed files instead of failing (#POLY-154)
- Fix snapshots skipping files without a file extension on macOS and Linux (#POLY-154)

### Added

- Add a modpack's bundled icon as the instance icon when the pack ships one
- Introduce persistent skip and hold update policies for bulk package updates with changelog previews in the review modal and per-package policy indicators and editing (#89, #POLY-159)

### Changed

- Update marketplace package previews to expand the details panel by default and show a dependency count badge and a missing required dependencies warning (#86)
- Reduce the installed size by over 40% through release-only trimming that keeps unannotated third-party assemblies intact
- Improve memory efficiency when browsing many image thumbnails
- Change instance setup group headers to toggle collapse on any click and open group details from a trailing info button

### Removed

-
