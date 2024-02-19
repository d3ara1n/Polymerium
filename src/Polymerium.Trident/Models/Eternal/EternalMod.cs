namespace Polymerium.Trident.Models.Eternal
{
    public struct EternalMod
    {
        public uint Id { get; init; }
        public uint GameId { get; init; }
        public string Name { get; init; }
        public string Slug { get; init; }
        public object Links { get; init; }
        public string Summary { get; init; }
        public int Status { get; init; }
        public uint DownloadCount { get; init; }
        public bool IsFeatured { get; init; }
        public uint PrimaryCategoryId { get; init; }
        public EternalModCategory[] Categories { get; init; }
        public uint ClassId { get; init; }
        public EternalModAuthor[] Authors { get; init; }
        public EternalModLogo? Logo { get; init; }
        public EternalScreenshot[] Screenshots { get; init; }
        public uint MainFileId { get; init; }
        public EternalModLatestFile[] LatestFiles { get; init; }
        public EternalModLatestFileIndex[] LatestFilesIndexes { get; init; }
        public DateTimeOffset DateCreated { get; init; }
        public DateTimeOffset DateModified { get; init; }
        public DateTimeOffset DateReleased { get; init; }
        public bool? AllowModDistribution { get; init; }
        public uint GamePopularityRank { get; init; }
        public bool IsAvailable { get; init; }
        public int ThumbsUpCount { get; init; }
    }
}