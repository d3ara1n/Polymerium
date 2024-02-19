namespace Polymerium.Trident.Models.Minecraft
{
    public struct MinecraftVersionDownload
    {
        public string Sha1 { get; init; }
        public uint Size { get; init; }
        public Uri Url { get; init; }
    }
}