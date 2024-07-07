namespace Polymerium.Trident.Models.Minecraft;

public struct MinecraftVersionLoggingSide
{
    public string Argument { get; init; }
    public MinecraftVersionArtifact File { get; init; }
    public string Type { get; init; }
}