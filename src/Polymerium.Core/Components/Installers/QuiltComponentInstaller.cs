using System;

namespace Polymerium.Core.Components.Installers;

public sealed class QuiltComponentInstaller : FabricComponentInstallerBase
{
    protected override Uri LoaderMavenUrl => new("https://maven.quiltmc.org/repository/release/");
    protected override Uri IntermediaryMavenUrl => new("https://maven.fabricmc.net/");
    protected override Uri ManifestUrl => new("https://meta.quiltmc.org/v3/versions/loader/");
}
