namespace Polymerium.Trident.Models.PrismLauncher.Minecraft;

public struct PrismMinecraftIndex
{
    public int FormatVersion { get; init; }
    public string Name { get; init; }
    public string Uid { get; init; }
    public PrismMinecraftIndexVersion[] Versions { get; init; }
}