namespace Trident.Abstractions.Repositories.Resources;

public record Package(
    string Label,
    string Namespace,
    string Pid,
    string Vid,
    string Name,
    string Version,
    Uri? Thumbnail,
    string Author,
    string Summary,
    Uri Reference,
    ResourceKind Kind,
    ReleaseType ReleaseType,
    DateTimeOffset PublishedAt,
    Uri Download,
    string FileName,
    string? Hash,
    Requirement Requirements,
    IEnumerable<Dependency> Dependencies
    );