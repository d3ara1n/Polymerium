using Polymerium.Trident.Models.ModrinthApi;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Polymerium.Trident.Utilities;

public static class ModrinthHelper
{
    public const string LABEL = "modrinth";

    public const string OFFICIAL_ENDPOINT = "https://api.modrinth.com";
    public const string FAKE_ENDPOINT = "https://api.bbsmc.net";
    private const string OFFICIAL_PROJECT_URL = "https://modrinth.com/{0}/{1}";

    public const string RESOURCENAME_MODPACK = "modpack";
    public const string RESOURCENAME_MOD = "mod";
    public const string RESOURCENAME_RESOURCEPACK = "resourcepack";
    public const string RESOURCENAME_SHADERPACK = "shader";
    public const string RESOURCENAME_DATAPACK = "datapack";

    public static readonly IReadOnlyDictionary<string, string> MODLOADER_MAPPINGS = new Dictionary<string, string>
    {
        ["forge"] = LoaderHelper.LOADERID_FORGE,
        ["neoforge"] = LoaderHelper.LOADERID_NEOFORGE,
        ["fabric"] = LoaderHelper.LOADERID_FABRIC,
        ["quilt"] = LoaderHelper.LOADERID_QUILT
    };

    public static string? LoaderIdToName(string? id) =>
        id switch
        {
            LoaderHelper.LOADERID_FORGE => "forge",
            LoaderHelper.LOADERID_NEOFORGE => "neoforge",
            LoaderHelper.LOADERID_FABRIC => "fabric",
            LoaderHelper.LOADERID_QUILT => "quilt",
            _ => null
        };

    public static string ResourceKindToUrlKind(ResourceKind kind) =>
        kind switch
        {
            ResourceKind.Modpack => RESOURCENAME_MODPACK,
            ResourceKind.Mod => RESOURCENAME_MOD,
            ResourceKind.ResourcePack => RESOURCENAME_RESOURCEPACK,
            ResourceKind.ShaderPack => RESOURCENAME_SHADERPACK,
            ResourceKind.DataPack => RESOURCENAME_DATAPACK,
            _ => "unknown"
        };

    public static string? ResourceKindToType(ResourceKind? kind) =>
        kind switch
        {
            ResourceKind.Modpack => RESOURCENAME_MODPACK,
            ResourceKind.Mod => RESOURCENAME_MOD,
            ResourceKind.ResourcePack => RESOURCENAME_RESOURCEPACK,
            ResourceKind.ShaderPack => RESOURCENAME_SHADERPACK,
            ResourceKind.DataPack => RESOURCENAME_DATAPACK,
            _ => null
        };

    public static ResourceKind? ProjectTypeToKind(string? kind) =>
        kind switch
        {
            RESOURCENAME_MODPACK => ResourceKind.Modpack,
            RESOURCENAME_MOD => ResourceKind.Mod,
            RESOURCENAME_RESOURCEPACK => ResourceKind.ResourcePack,
            RESOURCENAME_SHADERPACK => ResourceKind.ShaderPack,
            RESOURCENAME_DATAPACK => ResourceKind.DataPack,
            _ => null
        };

    public static ReleaseType VersionTypeToReleaseType(string type) =>
        type switch
        {
            "release" => ReleaseType.Release,
            "beta" => ReleaseType.Beta,
            "alpha" => ReleaseType.Alpha,
            _ => ReleaseType.Release
        };

    public static Requirement ToRequirement(VersionInfo version) =>
        new(version.GameVersions,
        [
            .. version
                .Loaders.Select(x => MODLOADER_MAPPINGS.GetValueOrDefault(x))
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(x => x!)
        ]);

    public static IReadOnlyList<Dependency> ToDependencies(VersionInfo version) =>
    [
        .. version.Dependencies.Select(x => new Dependency(LABEL,
            null,
            x.ProjectId,
            x.VersionId,
            x.DependencyType != "optional"))
    ];

    public static Exhibit ToExhibit(SearchHit hit) =>
        new(LABEL,
            null,
            hit.ProjectId,
            hit.Title,
            hit.IconUrl,
            hit.Author,
            hit.Description,
            ProjectTypeToKind(hit.ProjectType) ?? ResourceKind.Unknown,
            hit.Downloads,
            hit.Categories,
            new(OFFICIAL_PROJECT_URL.Replace("{0}", hit.ProjectType).Replace("{1}", hit.Slug)),
            hit.DateCreated,
            hit.DateModified);

    public static Version ToVersion(VersionInfo version) =>
        new(LABEL,
            null,
            version.ProjectId,
            version.Id,
            version.VersionNumber,
            VersionTypeToReleaseType(version.VersionType),
            version.DatePublished,
            version.Downloads,
            ToRequirement(version),
            ToDependencies(version));

    public static Project ToProject(ProjectInfo project, MemberInfo? member)
    {
        var extracted = project.ProjectTypes.FirstOrDefault();
        var kind = ProjectTypeToKind(extracted) ?? ResourceKind.Unknown;
        return new(LABEL,
            null,
            project.Id,
            project.Name,
            project.IconUrl,
            member?.User.Name ?? member?.User.Username ?? project.TeamId,
            project.Summary,
            new(OFFICIAL_PROJECT_URL.Replace("{0}", extracted ?? "unknown").Replace("{1}", project.Slug)),
            kind,
            project.Categories,
            project.Published,
            project.Updated,
            project.Downloads,
            [.. project.Gallery.Select(x => new Project.Screenshot(x.Name, x.Url))]);
    }

    public static Package ToPackage(ProjectInfo project, VersionInfo version, MemberInfo? member)
    {
        var extracted = project.ProjectTypes.FirstOrDefault();
        var kind = ProjectTypeToKind(extracted) ?? ResourceKind.Unknown;
        var file = version.Files.FirstOrDefault(x => x.Primary)
                   ?? version.Files.FirstOrDefault()
                   ?? throw new ResourceNotFoundException($"{project.Id}/{version.Id} has no file available");
        return new(LABEL,
            null,
            project.Id,
            version.Id,
            project.Name,
            version.VersionNumber,
            project.IconUrl,
            member?.User.Name ?? member?.User.Username ?? project.TeamId,
            project.Summary,
            new(OFFICIAL_PROJECT_URL.Replace("{0}", extracted ?? "unknown").Replace("{1}", project.Slug)),
            kind,
            VersionTypeToReleaseType(version.VersionType),
            version.DatePublished,
            file.Url,
            file.Size,
            file.Filename,
            file.Hashes.Sha1,
            ToRequirement(version),
            ToDependencies(version));
    }

    public static IReadOnlyList<string> ToLoaderNames(IEnumerable<ModLoader> loaders) =>
    [
        .. loaders.Select(x => x.Name)
    ];

    public static IReadOnlyList<string> ToVersionNames(IEnumerable<GameVersion> versions) =>
    [
        .. versions.Where(x => x.VersionType == "release").Select(x => x.Version)
    ];

    public static string BuildFacets(string? projectType, string? gameVersion, string? modLoader)
    {
        var facets = new List<KeyValuePair<string, string>>();
        if (gameVersion != null) facets.Add(new("versions", gameVersion));

        if (modLoader != null) facets.Add(new("categories", modLoader));

        if (projectType != null) facets.Add(new("project_type", projectType));

        return "[" + string.Join(",", facets.Select(x => $"[\"{x.Key}:{x.Value}\"]")) + "]";
    }
}