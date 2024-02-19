namespace Polymerium.Trident.Models.Eternal
{
    public struct EternalModLogo
    {
        public uint Id { get; init; }
        public uint ModId { get; init; }
        public string Title { get; init; }
        public Uri ThumbnailUrl { get; init; }
        public Uri Url { get; init; }
    }
}