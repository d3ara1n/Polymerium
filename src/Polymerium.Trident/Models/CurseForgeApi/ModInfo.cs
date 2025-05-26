namespace Polymerium.Trident.Models.CurseForgeApi;

public record ModInfo(
    uint Id,
    uint GameId,
    string Name,
    string Slug,
    ModInfo.ModLinks Links,
    string Summary,
    ModInfo.ModStatus Status,
    ulong DownloadCount,
    bool IsFeatured,
    uint PrimaryCategoryId,
    IReadOnlyList<CategoryModel> Categories,
    uint? ClassId,
    IReadOnlyList<ModInfo.ModAuthor> Authors,
    ModInfo.ModAsset? Logo,
    IReadOnlyList<ModInfo.ModAsset> Screenshots,
    uint MainFileId,
    IReadOnlyList<FileInfo> LatestFiles,
    DateTimeOffset DateCreated,
    DateTimeOffset DateModified,
    DateTimeOffset DateReleased,
    bool? AllowModDistribution,
    uint GamePopularityRank,
    bool IsAvailable,
    uint ThumbUpCount,
    float? Rating)
{
    #region ModStatus enum

    public enum ModStatus
    {
        New = 1,
        ChangesRequired,
        UnderSoftReview,
        Approved,
        Rejected,
        ChangesMade,
        Inactive,
        Abandoned,
        Deleted,
        UnderReview
    }

    #endregion

    #region Nested type: ModAsset

    public record ModAsset(uint Id, uint ModId, string Title, string Description, Uri? ThumbnailUrl, Uri Url);

    #endregion

    #region Nested type: ModAuthor

    public record ModAuthor(uint Id, string Name, Uri Url);

    #endregion

    #region Nested type: ModLinks

    public record ModLinks(Uri? WebsiteUrl, Uri? WikiUrl, Uri? IssuesUrl, Uri? SourceUrl);

    #endregion
}