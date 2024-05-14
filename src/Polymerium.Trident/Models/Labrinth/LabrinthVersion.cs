using Polymerium.Trident.Helpers;
using Polymerium.Trident.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Models.Labrinth;

public struct LabrinthVersion
{
    public string Name { get; init; }
    public string VersionNumber { get; init; }
    public string Changelog { get; init; }
    public IEnumerable<LabrinthVersionDependency> Dependencies { get; init; }
    public IEnumerable<string> GameVersions { get; init; }
    public string VersionType { get; init; }
    public IEnumerable<string> Loaders { get; init; }
    public bool Featured { get; init; }
    public string Status { get; init; }
    public string RequestedStatus { get; init; }
    public string Id { get; init; }
    public string ProjectId { get; init; }
    public string AuthorId { get; init; }
    public DateTimeOffset DatePublished { get; init; }
    public long Downloads { get; init; }
    public Uri? ChangelogUrl { get; init; }
    public IEnumerable<LabrinthVersionFile> Files { get; init; }

    public ReleaseType ExtractReleaseType()
    {
        return VersionType switch
        {
            "release" => ReleaseType.Release,
            "beta" => ReleaseType.Beta,
            "alpha" => ReleaseType.Alpha,
            _ => throw new NotSupportedException()
        };
    }

    public Requirement ExtractRequirement()
    {
        return new Requirement(GameVersions,
            Loaders.Where(x => x != "minecraft" && x != "datapack").Select(x =>
                ModrinthHelper.MODLOADER_MAPPINGS.ContainsKey(x)
                    ? ModrinthHelper.MODLOADER_MAPPINGS[x]
                    : throw new NotSupportedException()));
    }

    public IEnumerable<Dependency> ExtractDependencies()
    {
        return Dependencies.Where(x => x.ProjectId != null).Select(x =>
            new Dependency(RepositoryLabels.MODRINTH, x.ProjectId!, x.VersionId, x.DependencyType == "required"));
    }
}