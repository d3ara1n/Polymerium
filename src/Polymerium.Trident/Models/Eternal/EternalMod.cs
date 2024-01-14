namespace Polymerium.Trident.Models.Eternal;

public struct EternalMod
{
    public uint Id { get; set; }
    public uint GameId { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    public object Links { get; set; }
    public string Summary { get; set; }
    public int Status { get; set; }
    public uint DownloadCount { get; set; }
    public bool IsFeatured { get; set; }
    public uint PrimaryCategoryId { get; set; }
    public IEnumerable<EternalModCategory> Categories { get; set; }
    public uint ClassId { get; set; }
    public IEnumerable<EternalModAuthor> Authors { get; set; }
    public EternalModLogo? Logo { get; set; }
    public IEnumerable<EternalScreenshot> Screenshots { get; set; }
    public uint MainFileId { get; set; }
    public IEnumerable<EternalModLatestFile> LatestFiles { get; set; }
    public IEnumerable<EternalModLatestFileIndex> LatestFilesIndexes { get; set; }
    public DateTimeOffset DateCreated { get; set; }
    public DateTimeOffset DateModified { get; set; }
    public DateTimeOffset DateReleased { get; set; }
    public bool? AllowModDistribution { get; set; }
    public uint GamePopularityRank { get; set; }
    public bool IsAvailable { get; set; }
    public int ThumbsUpCount { get; set; }
}