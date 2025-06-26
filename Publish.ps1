dotnet publish -c Release --self-contained -r win-x64 src/Polymerium.App/Polymerium.App.csproj
vpk vpk pack --packId Polymerium --packVersion 0.7.0-rc.0 --packDir src/Polymerium.App/bin/Release/net9.0/win-x64/publish --mainExe Polymerium.App.exe
