using Polymerium.Trident.Clients;
using Polymerium.Trident.Models.CurseForgeApi;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Polymerium.Trident.Services;

public class CurseForgeService(ICurseForgeClient client)
{
    public const string LABEL = "curseforge";

    public const string API_KEY = "$2a$10$cjd5uExXA6oMi3lSnylNC.xsFJiujI8uQ/pV1eGltFe/hlDO2mjzm";
    public const string ENDPOINT = "https://api.curseforge.com";
    public const string PROJECT_URL = "https://www.curseforge.com/minecraft/{0}/{1}";

    public const uint GAME_ID = 432;
    public const uint CLASSID_MODPACK = 4471;
    public const uint CLASSID_MOD = 6;
    public const uint CLASSID_WORLD = 17;
    public const uint CLASSID_DATAPACK = 4546;
    public const uint CLASSID_SHADERPACK = 6552;
    public const uint CLASSID_RESOURCEPACK = 12;

    public static readonly IReadOnlyDictionary<string, string> LOADER_MAPPINGS = new Dictionary<string, string>
    {
        ["Forge"] = LoaderHelper.LOADERID_FORGE,
        ["NeoForge"] = LoaderHelper.LOADERID_NEOFORGE,
        ["Fabric"] = LoaderHelper.LOADERID_FABRIC,
        ["Quilt"] = LoaderHelper.LOADERID_QUILT,
        ["LiteLoader"] = "LiteLoader",
        ["Cauldron"] = "Cauldron",
        ["Any"] = "Any"
    };

    public static ModLoaderTypeModel? LoaderIdToType(string? loader) =>
        loader switch
        {
            LoaderHelper.LOADERID_NEOFORGE => ModLoaderTypeModel.NeoForge,
            LoaderHelper.LOADERID_FORGE => ModLoaderTypeModel.Forge,
            LoaderHelper.LOADERID_FABRIC => ModLoaderTypeModel.Fabric,
            LoaderHelper.LOADERID_QUILT => ModLoaderTypeModel.Quilt,
            _ => null
        };

    public static uint? ResourceKindToClassId(ResourceKind? kind) =>
        kind switch
        {
            ResourceKind.Modpack => CLASSID_MODPACK,
            ResourceKind.Mod => CLASSID_MOD,
            ResourceKind.ResourcePack => CLASSID_RESOURCEPACK,
            ResourceKind.ShaderPack => CLASSID_SHADERPACK,
            ResourceKind.World => CLASSID_WORLD,
            ResourceKind.DataPack => CLASSID_DATAPACK,
            _ => null
        };

    public static ResourceKind? ClassIdToResourceKind(uint? classId) =>
        classId switch
        {
            CLASSID_MODPACK => ResourceKind.Modpack,
            CLASSID_MOD => ResourceKind.Mod,
            CLASSID_RESOURCEPACK => ResourceKind.ResourcePack,
            CLASSID_SHADERPACK => ResourceKind.ShaderPack,
            CLASSID_WORLD => ResourceKind.World,
            CLASSID_DATAPACK => ResourceKind.DataPack,
            _ => null
        };

    public static string ResourceKindToUrlKind(ResourceKind? kind) =>
        kind switch
        {
            ResourceKind.Modpack => "modpacks",
            ResourceKind.Mod => "mc-mods",
            ResourceKind.World => "worlds",
            ResourceKind.ResourcePack => "texture-packs",
            ResourceKind.ShaderPack => "shaders",
            ResourceKind.DataPack => "data-packs",
            _ => "unknown"
        };

    public static ReleaseType ToReleaseType(FileModel.FileReleaseType type) =>
        type switch
        {
            FileModel.FileReleaseType.Alpha => ReleaseType.Alpha,
            FileModel.FileReleaseType.Beta => ReleaseType.Beta,
            FileModel.FileReleaseType.Release => ReleaseType.Release,
            _ => ReleaseType.Release
        };

    public static Uri ToDownloadUrl(FileModel file) =>
        file.DownloadUrl
     ?? new
            Uri($"https://edge.forgecdn.net/files/{file.Id / 1000}/{file.Id % 1000}/{Uri.EscapeDataString(file.FileName)}");

    public static string? ToSha1(FileModel file) =>
        file.Hashes.Any(x => x.Algo == FileModel.FileHash.HashAlgo.Sha1)
            ? file.Hashes.First(x => x.Algo == FileModel.FileHash.HashAlgo.Sha1).Value
            : null;

    public static Requirement ToRequirement(FileModel file)
    {
        List<string> gameReq = [];
        List<string> loaderReq = [];
        foreach (var version in file.GameVersions.Where(x => x != "Client" && x != "Server"))
            if (LOADER_MAPPINGS.TryGetValue(version, out var loader))
                loaderReq.Add(loader);
            else
                gameReq.Add(version);

        return new Requirement(gameReq, loaderReq);
    }

    public static IEnumerable<Dependency> ToDependencies(FileModel file) =>
        file
           .Dependencies
           .Where(x => x.RelationType is FileModel.FileDependency.FileRelationType.RequiredDependency
                                      or FileModel.FileDependency.FileRelationType.OptionalDependency)
           .Select(x => new Dependency(LABEL,
                                       null,
                                       x.ModId.ToString(),
                                       null,
                                       x.RelationType == FileModel.FileDependency.FileRelationType.RequiredDependency));


    public static Exhibit ToExhibit(ModModel model) =>
        new(LABEL,
            null,
            model.Id.ToString(),
            model.Name,
            model.Logo?.ThumbnailUrl?.IsAbsoluteUri is false ? model.Logo.Url : model.Logo?.ThumbnailUrl,
            model.Authors.Select(x => x.Name).FirstOrDefault() ?? "Anonymous",
            model.Summary,
            ClassIdToResourceKind(model.ClassId) ?? ResourceKind.Unknown,
            model.DownloadCount,
            model.Categories.Select(x => x.Name).ToList(),
            model.Links.WebsiteUrl
         ?? new Uri(PROJECT_URL.Replace("{0}", ResourceKindToUrlKind(ClassIdToResourceKind(model.ClassId)))),
            model.DateCreated,
            model.DateModified);


    public static Package ToPackage(ModModel mod, FileModel file) =>
        new(LABEL,
            null,
            mod.Id.ToString(),
            file.Id.ToString(),
            mod.Name,
            file.DisplayName,
            mod.Logo?.ThumbnailUrl?.IsAbsoluteUri is false ? mod.Logo.Url : mod.Logo?.ThumbnailUrl,
            mod.Authors.Select(x => x.Name).FirstOrDefault() ?? "Anonymous",
            mod.Summary,
            mod.Links.WebsiteUrl
         ?? new Uri(PROJECT_URL.Replace("{0}", ResourceKindToUrlKind(ClassIdToResourceKind(mod.ClassId)))),
            ClassIdToResourceKind(mod.ClassId) ?? ResourceKind.Unknown,
            ToReleaseType(file.ReleaseType),
            file.FileDate,
            ToDownloadUrl(file),
            file.FileLength,
            file.FileName,
            ToSha1(file),
            ToRequirement(file),
            ToDependencies(file));

    public static Version ToVersion(FileModel file) =>
        new(LABEL,
            null,
            file.ModId.ToString(),
            file.Id.ToString(),
            file.DisplayName.Trim(),
            ToReleaseType(file.ReleaseType),
            file.FileDate,
            file.DownloadCount,
            ToRequirement(file),
            ToDependencies(file));


    public static Project ToProject(ModModel model) =>
        new(LABEL,
            null,
            model.Id.ToString(),
            model.Name,
            model.Logo?.ThumbnailUrl?.IsAbsoluteUri is false ? model.Logo.Url : model.Logo?.ThumbnailUrl,
            model.Authors.Select(x => x.Name).FirstOrDefault() ?? "Anonymous",
            model.Summary,
            model.Links.WebsiteUrl
         ?? new Uri(PROJECT_URL
                   .Replace("{0}", ResourceKindToUrlKind(ClassIdToResourceKind(model.ClassId)))
                   .Replace("{1}", model.Slug)),
            ClassIdToResourceKind(model.ClassId) ?? ResourceKind.Unknown,
            model.Categories.Select(x => x.Name).ToList(),
            model.DateCreated,
            model.DateModified,
            model.DownloadCount,
            model.Screenshots.Select(x => new Project.Screenshot(x.Title, x.Url)).ToList());

    public async Task<string> GetModDescriptionAsync(uint modId) =>
        (await client.GetModDescriptionAsync(modId).ConfigureAwait(false)).Data;

    public async Task<string> GetModFileChangelogAsync(uint modId, uint fileId) =>
        (await client.GetModFileChangelogAsync(modId, fileId).ConfigureAwait(false)).Data;

    public async Task<IReadOnlyList<string>> GetGameVersionsAsync()
    {
        var types = (await client.GetVersionTypesAsync().ConfigureAwait(false))
                   .Data.Where(x => x.Status == 1 && x.Name.StartsWith("Minecraft"))
                   .Select(x => x.Id);
        var res = await client.GetVersionsAsync().ConfigureAwait(false);
        var versions = res.Data.Where(x => types.Contains(x.Type)).SelectMany(x => x.Versions).ToList();
        return versions;
    }

    public async Task<SearchResponse<ModModel>> SearchAsync(
        string searchFilter,
        uint? classId,
        string? gameVersion,
        ModLoaderTypeModel? modLoader,
        uint index = 0,
        uint pageSize = 50) =>
        await client
             .SearchModsAsync(searchFilter, classId, gameVersion, modLoader, index: index, pageSize: pageSize)
             .ConfigureAwait(false);

    public async Task<ModModel> GetModAsync(uint modId)
    {
        var rv = await client.GetModAsync(modId).ConfigureAwait(false);
        return rv.Data;
    }


    public async Task<FileModel> GetModFileAsync(uint modId, uint fileId)
    {
        var rv = await client.GetModFileAsync(modId, fileId).ConfigureAwait(false);
        return rv.Data;
    }

    public async Task<ArrayResponse<FileModel>> GetModFilesAsync(
        uint modId,
        string? gameVersion,
        ModLoaderTypeModel? modLoader,
        uint index = 0,
        uint count = 50)
    {
        var rv = await client.GetModFilesAsync(modId, gameVersion, modLoader, index, count).ConfigureAwait(false);
        return rv;
    }
}