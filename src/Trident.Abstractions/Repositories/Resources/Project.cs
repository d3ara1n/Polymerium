namespace Trident.Abstractions.Repositories.Resources;

public record Project(
    string Label,
    string? Namespace,
    string ProjectId,
    Uri? Thumbnail,
    string Author,
    string Summary,
    Uri Reference,
    ResourceKind Kind,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    uint DownloadCount,
    string Description,
    IEnumerable<Project.Screenshot> Gallery)
{
    public record Screenshot(string Title, Uri Url);
}