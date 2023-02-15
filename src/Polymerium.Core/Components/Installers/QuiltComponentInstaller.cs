using System;

namespace Polymerium.Core.Components.Installers;

public sealed class QuiltComponentInstaller : FabricComponentInstallerBase
{
    protected override Uri MavenUrl => new("https://maven.fabricmc.net/");
    protected override Uri ManifestUrl => new("https://meta.quiltmc.org/v3/versions/loader/");
}