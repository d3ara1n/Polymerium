namespace Polymerium.Trident.Models.PrismLauncher.Minecraft;

public struct PrismMinecraftVersionArtifact
{
    public string Sha1 { get; init; }
    public uint Size { get; init; }
    public Uri Url { get; init; }
}