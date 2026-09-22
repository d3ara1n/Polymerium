## [Unreleased]

### ✨ Highlights ✨

- Add a development overview widget that reports the modpack development state of an instance and hides the states with nothing to report in its compact form
- Rework modpack export around a shared instance overview and metadata options for each format

### Fixed

- Fix instance deployment failing with a reset prompt when operating system metadata files such as .DS_Store appear in the instance directories

### Added

- Add a development overview widget that reports the modpack development state of an instance and hides the states with nothing to report in its compact form

### Changed

- Improve instance resource preparation with cached readiness checks on the instance home page (#88)
- Improve instance deployment to keep managed files consistent across instance lifecycle operations (#POLY-179)
- Change Java deployment to prepare a shared matching runtime independently of custom launch preferences
- Change the built-in widgets to follow the interface language
- Change the modpack Workspace file comparison to run without blocking the interface
- Rework modpack export around a shared instance overview and metadata options for each format (#POLY-172)
- Improve update notes with expandable sections and visually emphasized highlights

### Removed

- Remove the unset account indicator from the instance home page
