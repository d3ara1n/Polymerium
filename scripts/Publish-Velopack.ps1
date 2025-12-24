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

# Detect current operating system
# PowerShell Core (6+) has $IsWindows, $IsLinux, $IsMacOS
# Windows PowerShell (5.1) doesn't have these, so we check for them
if ($null -eq $IsWindows) {
    # Running on Windows PowerShell 5.1
    $IsWindows = $true
    $IsLinux = $false
    $IsMacOS = $false
}

$CurrentOS = if ($IsWindows) { "win" } elseif ($IsLinux) { "linux" } elseif ($IsMacOS) { "osx" } else { "unknown" }

# Determine target OS from RID
$TargetOS = if ($Rid -like "win-*") { "win" } elseif ($Rid -like "linux-*") { "linux" } elseif ($Rid -like "osx-*") { "osx" } else { "unknown" }

# Determine if cross-platform packing is needed
$IsCrossPlatform = $CurrentOS -ne $TargetOS

# Determine executable name and icon based on target runtime
if ($Rid -like "win-*") {
    $ExeName = "Polymerium.App.exe"
    $IconPath = "./src/Polymerium.App/Assets/Icon.ico"
} else {
    $ExeName = "Polymerium.App"
    $IconPath = "./src/Polymerium.App/Assets/Icon.png"
}

$PackDir = "Publish/$Rid"

Write-Host "Packing Polymerium with Velopack..."
Write-Host "Version: $Version"
Write-Host "Runtime: $Rid"
Write-Host "Framework: $DotnetFramework"
Write-Host "Pack Directory: $PackDir"
Write-Host "Executable: $ExeName"
Write-Host "Icon: $IconPath"
Write-Host "Current OS: $CurrentOS"
Write-Host "Target OS: $TargetOS"
Write-Host "Cross-platform: $IsCrossPlatform"

# Build vpk command arguments
$VpkArgs = @(
    "pack",
    "--runtime", $Rid,
    "--packId", "Polymerium",
    "--packVersion", $Version,
    "--packDir", $PackDir,
    "--releaseNotes", "README.md",
    "--mainExe", $ExeName,
    "--icon", $IconPath
)

# For cross-platform packing, we need to use vpk [platform] pack syntax
# Velopack requires specifying the target platform in brackets when cross-compiling
if ($IsCrossPlatform) {
    Write-Host "Cross-platform packing detected: $CurrentOS -> $TargetOS"
    Write-Host "Running: vpk [$TargetOS] $($VpkArgs -join ' ')"

    # Use Start-Process to handle the bracket syntax properly
    $VpkArgsString = "[$TargetOS] " + ($VpkArgs -join ' ')
    $process = Start-Process -FilePath "vpk" -ArgumentList $VpkArgsString -NoNewWindow -PassThru -Wait
    $exitCode = $process.ExitCode
} else {
    Write-Host "Running: vpk $($VpkArgs -join ' ')"
    & vpk @VpkArgs
    $exitCode = $LASTEXITCODE
}

if ($exitCode -ne 0) {
    Write-Error "vpk pack failed with exit code $exitCode"
    exit $exitCode
}

Write-Host "Velopack packing completed successfully."
