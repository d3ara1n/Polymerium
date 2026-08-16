## [Unreleased]

### ✨ Highlights ✨

- Fix a failed modpack update partially deleting or corrupting the instance
- Reduce the installed size by over 40% through release-only trimming that keeps unannotated third-party assemblies intact
- Improve memory efficiency when browsing many image thumbnails

### Fixed

- Fix a failed modpack update partially deleting or corrupting the instance (#83, #POLY-153)
- Fix updating an instance that has never been launched (#POLY-153)
- Fix restoring a deleted snapshot wiping the instance's managed files instead of failing (#POLY-154)
- Fix snapshots skipping files without a file extension on macOS and Linux (#POLY-154)

### Added

- Add a modpack's bundled icon as the instance icon when the pack ships one

### Changed

- Reduce the installed size by over 40% through release-only trimming that keeps unannotated third-party assemblies intact
- Improve memory efficiency when browsing many image thumbnails

### Removed

-
