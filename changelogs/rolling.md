## [Unreleased]

### ✨ Highlights ✨

- Add optional external launch plans that customize Minecraft startup configuration and remain part of instance imports and snapshots
- Add drag-and-drop group reordering and quick package assignment to collections on the instance setup page (#POLY-148, #POLY-141)

### Fixed

- Fix the check integrity action not performing a full validation pass
- Fix bundled Java runtime metadata being discarded on every deployment and refetched
- Fix update checks and downloads on the GitHub update channel ignoring the network proxy settings
- Fix a crash when opening the setup page of an instance after importing a package list from a file (#POLY-161, #POLYMERIUM-2G)
- Fix the launcher crashing right after a deployment failed, instead of reporting why it failed
- Fix importing a modpack whose archive has an extra folder wrapping its contents ending up with no packages
- Fix newly created instances not having the default account selected
- Fix launching an instance whose selected account was deleted doing nothing instead of showing the account selection prompt

### Added

- Add optional external launch plans that customize Minecraft startup configuration and remain part of instance imports and snapshots (#92, #POLY-165)
- Add drag-and-drop group reordering and quick package assignment to collections on the instance setup page (#POLY-148, #POLY-141)

### Changed

- Improve the blurred backdrop behind dialogs, sidebars and toasts to follow background motion at full frame rate and avoid stretching when the window is resized
- Improve the speed of play-time statistics and favorites search and prevent instance tags from being lost when saving is interrupted

### Removed

- Remove the fast launch option that skipped file validation before starting the game
- Remove an internal network proxy setting key that had no effect
