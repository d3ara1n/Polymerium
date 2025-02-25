namespace Polymerium.Trident.Models.CurseForgeApi;

public record ModModel(
    uint Id,
    uint GameId,
    string Name,
    string Slug,
    ModModel.ModLinks Links,
    string Summary,
    ModModel.ModStatus Status,
    ulong DownloadCount,
    bool IsFeatured,
    uint PrimaryCategoryId,
    IReadOnlyList<CategoryModel> Categories,
    uint? ClassId,
    IReadOnlyList<ModModel.ModAuthor> Authors,
    ModModel.ModAsset Logo,
    IReadOnlyList<ModModel.ModAsset> Screenshots,
    uint MainFileId,
    DateTimeOffset DateModified,
    DateTimeOffset DateCreated)
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

    public record ModAsset(uint Id, uint ModId, string Title, string Description, Uri ThumbnailUrl, Uri Url);

    #endregion

    #region Nested type: ModAuthor

    public record ModAuthor(uint Id, string Name, Uri Url);

    #endregion

    #region Nested type: ModLinks

    public record ModLinks(Uri? WebsiteUrl, Uri? WikiUrl, Uri? IssuesUrl, Uri? SourceUrl);

    #endregion
}