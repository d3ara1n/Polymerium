namespace Trident.Abstractions.Resources;

// 用于部署和用户界面展示
// 从 IRepository.Resolve(string projectId, string? versionId, Filter filter)

public record Package(
    string ProjectId,
    string ProjectName,
    string VersionId,
    string VersionName,
    Uri? Thumbnail,
    string Author,
    string Summary,
    Uri Reference,
    ResourceKind Kind,
    ReleaseType ReleaseType,
    DateTimeOffset PublishedAt,
    string FileName,
    Uri Download,
    string? Hash,
    Requirement Requirements,
    IEnumerable<Dependency> Dependencies)
{
}