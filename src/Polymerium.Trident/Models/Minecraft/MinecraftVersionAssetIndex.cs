namespace Polymerium.Trident.Models.Minecraft
{
    public struct MinecraftVersionAssetIndex
    {
        public string Id { get; init; }
        public string Sha1 { get; init; }
        public uint Size { get; init; }
        public uint TotalSize { get; init; }
        public Uri Url { get; init; }
    }
}