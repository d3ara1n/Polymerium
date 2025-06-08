using Trident.Abstractions.Utilities;

namespace Trident.Abstractions.Repositories.Resources;

public record Package(
    string Label,
    string? Namespace,
    string ProjectId,
    string VersionId,
    string ProjectName,
    string VersionName,
    Uri? Thumbnail,
    string Author,
    string Summary,
    Uri Reference,
    ResourceKind Kind,
    ReleaseType ReleaseType,
    DateTimeOffset PublishedAt,
    Uri Download,
    ulong Size,
    string FileName,
    string? Sha1,
    Requirement Requirements,
    IReadOnlyList<Dependency> Dependencies)
{
    public override string ToString() => PackageHelper.ToPurl(this);
}