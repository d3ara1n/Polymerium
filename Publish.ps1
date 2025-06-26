# Get version from GitVersion
$gitVersionOutput = dotnet gitversion | ConvertFrom-Json
$version = $gitVersionOutput.SemVer

Write-Host "Building version: $version" -ForegroundColor Green

dotnet publish -c Release --self-contained -r win-x64 src/Polymerium.App/Polymerium.App.csproj
vpk pack --packId Polymerium --packVersion $version --packDir src/Polymerium.App/bin/Release/net9.0/win-x64/publish --mainExe Polymerium.App.exe

Write-Host "Build completed for version: $version" -ForegroundColor Green
