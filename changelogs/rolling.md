## [Unreleased]

### ✨ Highlights ✨

- Rework the settings navigation into an inline sidebar
- Block launching a second instance and instead bring the already running window to the foreground
- Make deleting, resetting, or unlinking an instance require per-action confirmation instead of a shared unlock
- Prevent crashes when triggering a destructive action twice or with stale data by making profile removal idempotent

### Fixed

- Attempt to fix instance icons and settings disappearing after an update restart by freezing the data root at first access
- Fix directory-membership checks matching sibling folders that share a name prefix
- Fix path and file name comparisons remaining case-sensitive on macOS
- Prevent crashes when triggering a destructive action twice or with stale data by making profile removal idempotent

### Added

- Block launching a second instance and instead bring the already running window to the foreground

### Changed

- Rework the settings navigation into an inline sidebar
- Update file picking to return the on-disk casing of matched files
- Make deleting, resetting, or unlinking an instance require per-action confirmation instead of a shared unlock
- Wrap the about dialog link row so buttons no longer overflow on narrow windows

### Removed

-
