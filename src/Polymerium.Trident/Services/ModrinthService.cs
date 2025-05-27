using Polymerium.Trident.Clients;
using Polymerium.Trident.Models.ModrinthApi;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;

namespace Polymerium.Trident.Services;

public class ModrinthService(IModrinthClient client)
{
    public const string LABEL = "modrinth";

    public const string ENDPOINT = "https://api.modrinth.com";
    private const string PROJECT_URL = "https://modrinth.com/{0}/{1}";

    private const string RESOURCENAME_MODPACK = "modpack";
    private const string RESOURCENAME_MOD = "mod";
    private const string RESOURCENAME_RESOURCEPACK = "resourcepack";
    private const string RESOURCENAME_SHADERPACK = "shader";
    private const string RESOURCENAME_DATAPACK = "datapack";

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
        uint limit = 20)
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
}