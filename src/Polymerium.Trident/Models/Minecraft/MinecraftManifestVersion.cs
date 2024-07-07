namespace Polymerium.Trident.Models.Minecraft;

public struct MinecraftManifestVersion
{
    public string Id { get; init; }
    public MinecraftReleaseType Type { get; init; }
    public Uri Url { get; init; }
    public DateTimeOffset Time { get; init; }
    public DateTimeOffset ReleaseTime { get; init; }
}