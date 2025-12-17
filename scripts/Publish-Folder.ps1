param(
    [Parameter(Mandatory = $true)]
    [string]$Rid
)

$ErrorActionPreference = "Stop"

$ProjectPath = "src/Polymerium.App/Polymerium.App.csproj"

Write-Host "Publishing Polymerium.App for $Rid..."

dotnet publish -c Release --self-contained -r $Rid $ProjectPath -o "Publish/$Rid"

if ($LASTEXITCODE -ne 0)
{
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Publish completed successfully."
