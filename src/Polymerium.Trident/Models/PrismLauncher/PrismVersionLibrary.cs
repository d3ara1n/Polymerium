namespace Polymerium.Trident.Models.PrismLauncher
{
    public struct PrismVersionLibrary
    {
        public PrismVersionLibraryDownloads Downloads { get; init; }
        public PrismVersionLibraryExtract? Extract { get; init; }
        public string Name { get; init; }
        public PrismMinecraftVersionLibraryNatives? Natives { get; init; }
        public PrismVersionLibraryRule[]? Rules { get; init; }
    }
}