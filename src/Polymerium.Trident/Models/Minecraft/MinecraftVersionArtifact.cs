namespace Polymerium.Trident.Models.Minecraft;

public struct MinecraftVersionArtifact
{
    public string Path { get; init; }
    public string Sha1 { get; init; }
    public uint Size { get; init; }
    public Uri Url { get; init; }
}