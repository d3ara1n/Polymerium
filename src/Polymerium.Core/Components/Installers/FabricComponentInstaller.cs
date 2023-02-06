using System;

namespace Polymerium.Core.Components.Installers;

public sealed class FabricComponentInstaller : FabricComponentInstallerBase
{
    protected override Uri MavenUrl => new("https://maven.fabricmc.net/");
    protected override Uri ManifestUrl => new("https://meta.fabricmc.net/v2/versions/loader/");
}