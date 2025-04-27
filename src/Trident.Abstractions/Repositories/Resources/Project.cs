namespace Trident.Abstractions.Repositories.Resources;

public record Project(
    string Label,
    string? Namespace,
    string ProjectId,
    string ProjectName,
    Uri? Thumbnail,
    string Author,
    string Summary,
    Uri Reference,
    ResourceKind Kind,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    ulong DownloadCount,
    IEnumerable<Project.Screenshot> Gallery)
{
    #region Nested type: Screenshot

    public record Screenshot(string Title, Uri Url);

    #endregion
}