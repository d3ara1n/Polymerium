namespace Trident.Abstractions.Repositories.Resources;

public record Exhibit(
    string Label,
    string? Namespace,
    string Pid,
    string Name,
    Uri? Thumbnail,
    string Author,
    string Summary,
    ResourceKind Kind,
    ulong DownloadCount,
    IReadOnlyList<string> Tags,
    Uri Reference,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);