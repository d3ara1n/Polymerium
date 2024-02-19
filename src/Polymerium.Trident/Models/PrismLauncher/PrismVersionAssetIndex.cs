namespace Polymerium.Trident.Models.PrismLauncher
{
    public struct PrismMinecraftVersionAssetIndex
    {
        public string Id { get; init; }
        public string Sha1 { get; init; }
        public uint Size { get; init; }
        public uint TotalSize { get; init; }
        public Uri Url { get; init; }
    }
}