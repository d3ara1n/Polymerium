namespace Polymerium.Trident.Models.Minecraft;

public struct MinecraftManifest
{
    public MinecraftManifestLatest Latest { get; init; }
    public MinecraftManifestVersion[] Versions { get; init; }
}