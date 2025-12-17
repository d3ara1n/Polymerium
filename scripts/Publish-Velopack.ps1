param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$Rid
)

$ErrorActionPreference = "Stop"

$ProjectPath = "src/Polymerium.App/Polymerium.App.csproj"

# Extract TargetFramework from csproj
$CsprojContent = Get-Content $ProjectPath -Raw
if ($CsprojContent -match '<TargetFramework>(net[0-9]+\.[0-9]+)</TargetFramework>') {
    $DotnetFramework = $Matches[1]
} else {
    Write-Error "Could not extract TargetFramework from $ProjectPath"
    exit 1
}

# Determine executable name based on runtime
if ($Rid -like "win-*") {
    $ExeName = "Polymerium.App.exe"
    $PackExtraArgs = @()
} else {
    $ExeName = "Polymerium.App"
    $PackExtraArgs = @("--icon", "./src/Polymerium.App/Assets/Icon.png")
}

$PackDir = "src/Polymerium.App/bin/Release/$DotnetFramework/$Rid/publish"

Write-Host "Packing Polymerium with Velopack..."
Write-Host "Version: $Version"
Write-Host "Runtime: $Rid"
Write-Host "Framework: $DotnetFramework"
Write-Host "Pack Directory: $PackDir"
Write-Host "Executable: $ExeName"

$VpkArgs = @(
    "pack",
    "--runtime", $Rid,
    "--packId", "Polymerium",
    "--packVersion", $Version,
    "--packDir", $PackDir,
    "--releaseNotes", "CHANGELOG.md",
    "--mainExe", $ExeName
) + $PackExtraArgs

Write-Host "Running: vpk $($VpkArgs -join ' ')"

& vpk @VpkArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "vpk pack failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Velopack packing completed successfully."
