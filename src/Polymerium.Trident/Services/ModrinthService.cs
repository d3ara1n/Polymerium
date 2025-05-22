using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;

namespace Polymerium.Trident.Services;

public class ModrinthService
{
    public const string LABEL = "modrinth";

    public const string ENDPOINT = "https://api.modrinth.com/v2";
    private const string PROJECT_URL = "https://modrinth.com/{0}/{1}";

    public static readonly IReadOnlyDictionary<string, string> MODLOADER_MAPPINGS = new Dictionary<string, string>
    {
        ["forge"] = LoaderHelper.LOADERID_FORGE,
        ["neoforge"] = LoaderHelper.LOADERID_NEOFORGE,
        ["fabric"] = LoaderHelper.LOADERID_FABRIC,
        ["quilt"] = LoaderHelper.LOADERID_QUILT
    };

    public static string ResourceKindToUrlKind(ResourceKind kind) =>
        kind switch
        {
            ResourceKind.Modpack => "modpack",
            ResourceKind.Mod => "mod",
            ResourceKind.ResourcePack => "resourcepack",
            ResourceKind.ShaderPack => "shader",
            ResourceKind.DataPack => "datapack",
            _ => "unknown"
        };
}