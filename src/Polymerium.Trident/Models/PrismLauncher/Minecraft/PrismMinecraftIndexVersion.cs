namespace Polymerium.Trident.Models.PrismLauncher.Minecraft;

public struct PrismMinecraftIndexVersion
{
    public bool Recommended { get; init; }
    public DateTimeOffset ReleaseTime { get; init; }
    public PrismMinecraftRequirement[] Requires { get; init; }
    public string Sha256 { get; init; }
    public PrismMinecraftReleaseType Type { get; init; }
    public string Version { get; init; }
}