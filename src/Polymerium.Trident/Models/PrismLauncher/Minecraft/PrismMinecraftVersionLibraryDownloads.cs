namespace Polymerium.Trident.Models.PrismLauncher.Minecraft;

public struct PrismMinecraftVersionLibraryDownloads
{
    public PrismMinecraftVersionArtifact? Artifact { get; init; }
    public IDictionary<string, PrismMinecraftVersionArtifact> Classifiers { get; init; }
}