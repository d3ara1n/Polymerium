[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$RootDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

$csharpierMissing = $null -eq (Get-Command "csharpier" -ErrorAction SilentlyContinue)
$xstylerMissing = $null -eq (Get-Command "xstyler" -ErrorAction SilentlyContinue)

if ($csharpierMissing -or $xstylerMissing) {
    Write-Host "Missing required tools:" -ForegroundColor Red
    if ($csharpierMissing) {
        Write-Host "  - csharpier: dotnet tool install csharpier -g" -ForegroundColor Yellow
    }
    if ($xstylerMissing) {
        Write-Host "  - xstyler:   dotnet tool install XamlStyler.Console -g" -ForegroundColor Yellow
    }
    exit 1
}

Write-Host "Formatting C# files with csharpier ..." -ForegroundColor Cyan
& csharpier format $RootDir

Write-Host "Formatting XAML files with xstyler ..." -ForegroundColor Cyan
& xstyler -d $RootDir -r -c (Join-Path $RootDir "Settings.XamlStyler") -l Minimal

Write-Host "Done." -ForegroundColor Green
