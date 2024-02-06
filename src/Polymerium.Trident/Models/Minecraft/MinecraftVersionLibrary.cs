namespace Polymerium.Trident.Models.Minecraft;

public struct MinecraftVersionLibrary
{
    public MinecraftVersionLibraryDownloads Downloads { get; init; }
    public MinecraftVersionLibraryExtract? Extract { get; init; }
    public string Name { get; init; }
    public MinecraftVersionLibraryNatives? Natives { get; init; }
    public MinecraftVersionRule[] Rules { get; init; }
}