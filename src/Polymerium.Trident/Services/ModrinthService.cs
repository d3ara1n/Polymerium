using Polymerium.Trident.Clients;
using Polymerium.Trident.Models.ModrinthApi;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Polymerium.Trident.Services;

public class ModrinthService(IModrinthClient client)
{
    public const string LABEL = "modrinth";

    public const string ENDPOINT = "https://api.modrinth.com";
    private const string PROJECT_URL = "https://modrinth.com/{0}/{1}";

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
            version
               .Loaders.Select(x => MODLOADER_MAPPINGS.GetValueOrDefault(x))
               .Where(x => !string.IsNullOrEmpty(x))
               .Select(x => x!)
               .ToList());

    public static IReadOnlyList<Dependency> ToDependencies(VersionInfo version) =>
        version
           .Dependencies
           .Select(x => new Dependency(LABEL, null, x.ProjectId, x.VersionId, x.DependencyType != "optional"))
           .ToList();

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
            new Uri(PROJECT_URL.Replace("{0}", hit.ProjectType).Replace("{1}", hit.Slug)),
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
        return new Project(LABEL,
                           null,
                           project.Id,
                           project.Name,
                           project.IconUrl,
                           member?.User.Name ?? member?.User.Username ?? project.TeamId,
                           project.Summary,
                           new Uri(PROJECT_URL.Replace("{0}", extracted ?? "unknown").Replace("{1}", project.Slug)),
                           kind,
                           project.Categories,
                           project.Published,
                           project.Updated,
                           project.Downloads,
                           project.Gallery.Select(x => new Project.Screenshot(x.Name, x.Url)).ToList());
    }

    public async Task<IReadOnlyList<string>> GetGameVersionsAsync()
    {
        var versions = await client.GetGameVersionsAsync().ConfigureAwait(false);
        return versions.Where(x => x.VersionType == "release").Select(x => x.Version).ToList();
    }

    public async Task<IReadOnlyList<string>> GetLoadersAsync()
    {
        var loaders = await client.GetLoadersAsync().ConfigureAwait(false);
        return loaders.Select(x => x.Name).ToList();
    }

    public Task<SearchResponse<SearchHit>> SearchAsync(
        string query,
        string? projectType,
        string? gameVersion = null,
        string? modLoader = null,
        uint offset = 0,
        uint limit = 10)
    {
        var facets = new List<KeyValuePair<string, string>>();
        if (gameVersion != null)
            facets.Add(new KeyValuePair<string, string>("versions", gameVersion));

        if (modLoader != null)
            facets.Add(new KeyValuePair<string, string>("categories", modLoader));
        if (projectType != null)
            facets.Add(new KeyValuePair<string, string>("project_type", projectType));

        return client.SearchAsync(query,
                                  "[" + string.Join(",", facets.Select(x => $"[\"{x.Key}:{x.Value}\"]")) + "]",
                                  offset: offset,
                                  limit: limit);
    }

    public Task<ProjectInfo> GetProjectAsync(string projectId) => client.GetProjectAsync(projectId);

    public Task<IReadOnlyList<MemberInfo>> GetTeamMembersAsync(string teamId) => client.GetTeamMembersAsync(teamId);

    public Task<IReadOnlyList<VersionInfo>> GetProjectVersionsAsync(
        string projectId,
        string? gameVersion,
        string? modLoader,
        uint offset = 0,
        uint limit = 10) =>
        client.GetProjectVersionsAsync(projectId,
                                       modLoader is not null ? [modLoader] : null,
                                       gameVersion is not null ? [gameVersion] : null,
                                       offset: offset,
                                       limit: limit);
}