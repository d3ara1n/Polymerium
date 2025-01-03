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
    DateTimeOffset DateCreated
)
{
    public record ModLinks(Uri? WebsiteUrl, Uri? WikiUrl, Uri? IssuesUrl, Uri? SourceUrl);

    public record ModAuthor(uint Id, string Name, Uri Url);

    public record ModAsset(uint Id, uint ModId, string Title, string Description, Uri ThumbnailUrl, Uri Url);

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
}