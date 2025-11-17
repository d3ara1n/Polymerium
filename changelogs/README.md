# Changelog Management

This project uses a rolling changelog system based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format.

## Workflow

### During Development

All changes should be documented in `changelogs/rolling.md` under the `[Unreleased]` section. The changelog follows this structure:

- **Highlights**: Major features or important changes worth highlighting
- **Added**: New features
- **Changed**: Changes in existing functionality
- **Deprecated**: Soon-to-be removed features
- **Removed**: Removed features
- **Fixed**: Bug fixes
- **Security**: Security-related changes

### During Release

When a new version is released (by pushing a tag like `v1.0.0`), the GitHub Actions workflow automatically:

1. **Extracts** the `[Unreleased]` section from `rolling.md`
2. **Creates** `changelog.md` with the release notes (used by Velopack for release notes)
3. **Archives** the unreleased changes in `rolling.md` with the version number and date
4. **Resets** the `[Unreleased]` section with a fresh template
5. **Commits** the changes back to the repository (only on Windows build to avoid conflicts)

## Manual Usage

You can also run the changelog script manually:

```powershell
# Extract and archive changelog for version 1.0.0
.\scripts\Update-Changelog.ps1 -Version "1.0.0"

# Extract, archive, and commit the changes
.\scripts\Update-Changelog.ps1 -Version "v1.0.0" -CommitChanges
```

## File Structure

- `changelogs/rolling.md` - The main changelog file with unreleased changes and version history
- `changelog.md` - Generated during release, contains only the current release notes
- `changelogs/v*.md` - Legacy changelog files (kept for historical reference)

## Example Entry

```markdown
## [Unreleased]

### Highlights
- Complete UI redesign with modern interface

### Added
- New dark mode theme
- Export functionality for user data

### Changed
- Improved performance of modpack loading

### Fixed
- Fixed crash when loading large modpacks
- Corrected display issues on high-DPI screens
```

## Migration from git-cliff

This project previously used `git-cliff` for automatic changelog generation from git commits. The new system provides:

- **Manual control**: Developers explicitly document changes
- **Better organization**: Clear categorization of changes
- **Highlights section**: Ability to emphasize important changes
- **Keep a Changelog standard**: Industry-standard format
