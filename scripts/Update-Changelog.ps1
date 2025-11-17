#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Extracts unreleased changelog entries and archives them for a new version release.

.DESCRIPTION
    This script processes the rolling changelog (changelogs/rolling.md) by:
    1. Extracting the [Unreleased] section to changelog.md for release notes
    2. Archiving the [Unreleased] section in rolling.md with the version number and date
    3. Creating a fresh [Unreleased] template in rolling.md
    4. Optionally committing the changes to the repository

.PARAMETER Version
    The version number for the release (e.g., "1.0.0" or "v1.0.0")

.PARAMETER CommitChanges
    If specified, commits the changelog changes to the repository

.EXAMPLE
    .\Update-Changelog.ps1 -Version "1.0.0"

.EXAMPLE
    .\Update-Changelog.ps1 -Version "v1.0.0" -CommitChanges
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $false)]
    [switch]$CommitChanges
)

$ErrorActionPreference = "Stop"

# Normalize version (remove 'v' prefix if present)
$normalizedVersion = $Version -replace '^v', ''
$versionWithV = if ($Version.StartsWith('v')) { $Version } else { "v$Version" }

# File paths
$rollingChangelog = Join-Path $PSScriptRoot ".." "changelogs" "rolling.md"
$outputChangelog = Join-Path $PSScriptRoot ".." "CHANGELOG.md"

# Verify rolling.md exists
if (-not (Test-Path $rollingChangelog)) {
    Write-Error "Rolling changelog not found at: $rollingChangelog"
    exit 1
}

Write-Host "Processing changelog for version $versionWithV..." -ForegroundColor Cyan

# Read the rolling changelog
$content = Get-Content $rollingChangelog -Raw -Encoding UTF8

# Extract the Unreleased section
$unreleasedPattern = '(?s)## \[Unreleased\](.*?)(?=\n## \[|$)'
if ($content -match $unreleasedPattern) {
    $unreleasedSection = $Matches[1].Trim()

    # Check if there's any actual content (not just empty sections)
    $hasContent = $unreleasedSection -match '[^\s\-]'

    if (-not $hasContent) {
        Write-Warning "No changes found in [Unreleased] section. Proceeding anyway..."
    }

    # Create the release notes for changelog.md
    $releaseDate = Get-Date -Format "yyyy-MM-dd"
    $releaseNotes = @"
# Changelog

## [$normalizedVersion] - $releaseDate

$unreleasedSection
"@

    # Write to changelog.md
    Set-Content -Path $outputChangelog -Value $releaseNotes -Encoding UTF8 -NoNewline
    Write-Host "✓ Created CHANGELOG.md with release notes" -ForegroundColor Green

    # Create the archived version entry
    $archivedEntry = @"
## [$normalizedVersion] - $releaseDate

$unreleasedSection
"@

    # Create new unreleased template
    $unreleasedTemplate = @"
## [Unreleased]

### Highlights
-

### Added
-

### Changed
-

### Removed
-

### Fixed
-
"@

    # Replace the Unreleased section with both the new template and archived version
    $newContent = $content -replace $unreleasedPattern, @"
$unreleasedTemplate

$archivedEntry
"@

    # Write back to rolling.md
    Set-Content -Path $rollingChangelog -Value $newContent -Encoding UTF8 -NoNewline
    Write-Host "✓ Updated rolling.md with archived version and new [Unreleased] template" -ForegroundColor Green

    # Commit changes if requested
    if ($CommitChanges) {
        Write-Host "`nCommitting changelog changes..." -ForegroundColor Cyan

        # Check if we're in a git repository
        $gitRoot = git rev-parse --show-toplevel 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Not in a git repository"
            exit 1
        }

        # Stage the changelog files
        git add $rollingChangelog $outputChangelog

        # Commit the changes
        $commitMessage = "chore(release): update changelog for $versionWithV"
        git commit -m $commitMessage

        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Committed changelog changes" -ForegroundColor Green
            Write-Host "  Commit message: $commitMessage" -ForegroundColor Gray
        } else {
            Write-Warning "Failed to commit changes. Please commit manually."
        }
    } else {
        Write-Host "`nChanges not committed. Use -CommitChanges to commit automatically." -ForegroundColor Yellow
    }

    Write-Host "`n✓ Changelog processing complete!" -ForegroundColor Green
    Write-Host "  - changelog.md: Release notes for $versionWithV" -ForegroundColor Gray
    Write-Host "  - rolling.md: Archived $versionWithV and reset [Unreleased]" -ForegroundColor Gray

} else {
    Write-Error "Could not find [Unreleased] section in rolling.md"
    exit 1
}
