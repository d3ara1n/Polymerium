namespace Polymerium.Trident.Models.PrismLauncher.Minecraft;

public struct PrismMinecraftVersionLibrary
{
    public PrismMinecraftVersionLibraryDownloads Downloads { get; init; }
    public PrismMinecraftVersionLibraryExtract? Extract { get; init; }
    public string Name { get; init; }
    public PrismMinecraftVersionLibraryNatives? Natives { get; init; }
    public PrismMinecraftVersionLibraryRule[]? Rules { get; init; }
}