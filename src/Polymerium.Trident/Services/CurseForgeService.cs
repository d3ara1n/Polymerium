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

    public ModLoaderTypeModel? LoaderIdToType(string? loader) =>
        loader switch
        {
            LoaderHelper.LOADERID_NEOFORGE => ModLoaderTypeModel.NeoForge,
            LoaderHelper.LOADERID_FORGE => ModLoaderTypeModel.Forge,
            LoaderHelper.LOADERID_FABRIC => ModLoaderTypeModel.Fabric,
            LoaderHelper.LOADERID_QUILT => ModLoaderTypeModel.Quilt,
            _ => null
        };

    public uint? ResourceKindToClassId(ResourceKind? kind) =>
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

    public ResourceKind? ClassIdToResourceKind(uint? classId) =>
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

    public ReleaseType ToReleaseType(FileModel.FileReleaseType type) =>
        type switch
        {
            FileModel.FileReleaseType.Alpha => ReleaseType.Alpha,
            FileModel.FileReleaseType.Beta => ReleaseType.Beta,
            FileModel.FileReleaseType.Release => ReleaseType.Release,
            _ => ReleaseType.Release
        };

    public Uri ToDownloadUrl(FileModel file) =>
        file.DownloadUrl
     ?? new
            Uri($"https://mediafilez.forgecdn.net/files/{file.Id / 1000}/{file.Id % 1000}/{Uri.EscapeDataString(file.FileName)}");

    public string? ToSha1(FileModel file) =>
        file.Hashes.Any(x => x.Algo == FileModel.FileHash.HashAlgo.Sha1)
            ? file.Hashes.First(x => x.Algo == FileModel.FileHash.HashAlgo.Sha1).Value
            : null;

    public Requirement ToRequirement(FileModel file)
    {
        List<string> gameReq = [];
        List<string> loaderReq = [];
        foreach (var version in file.GameVersions)
            if (LOADER_MAPPINGS.TryGetValue(version, out var loader))
                loaderReq.Add(loader);
            else
                gameReq.Add(version);

        return new Requirement(gameReq, loaderReq);
    }

    public IEnumerable<Dependency> ToDependencies(FileModel file) =>
        file
           .Dependencies
           .Where(x => x.RelationType is FileModel.FileDependency.FileRelationType.RequiredDependency
                                      or FileModel.FileDependency.FileRelationType.OptionalDependency)
           .Select(x => new Dependency(LABEL,
                                       null,
                                       x.ModId.ToString(),
                                       null,
                                       x.RelationType == FileModel.FileDependency.FileRelationType.RequiredDependency));


    public Exhibit ToExhibit(ModModel model) =>
        new(LABEL,
            null,
            model.Id.ToString(),
            model.Name,
            model.Logo?.ThumbnailUrl?.IsAbsoluteUri is false ? model.Logo.Url : model.Logo?.ThumbnailUrl,
            model.Authors.Select(x => x.Name).FirstOrDefault() ?? "??",
            model.Summary,
            ClassIdToResourceKind(model.ClassId) ?? ResourceKind.Unknown,
            model.DownloadCount,
            model.Categories.Select(x => x.Name).ToList(),
            model.Links.WebsiteUrl
         ?? new Uri(PROJECT_URL.Replace("{0}", ResourceKindToUrlKind(ClassIdToResourceKind(model.ClassId)))),
            model.DateCreated,
            model.DateModified);


    public Package ToPackage(ModModel mod, FileModel file) =>
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

    public Version ToVersion(FileModel file) =>
        new(LABEL,
            null,
            file.ModId.ToString(),
            file.Id.ToString(),
            file.DisplayName,
            ToReleaseType(file.ReleaseType),
            file.FileDate,
            file.DownloadCount,
            string.Empty);

    public async Task<IReadOnlyList<string>> GetGameVersionsAsync()
    {
        var types = (await client.GetVersionTypesAsync())
                   .Data.Where(x => x.Status == 1 && x.Name.StartsWith("Minecraft"))
                   .Select(x => x.Id);
        var res = await client.GetVersionsAsync();
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
        await client.SearchModsAsync(searchFilter, classId, gameVersion, modLoader, index: index, pageSize: pageSize);

    public async Task<ModModel> GetModAsync(uint modId)
    {
        var rv = await client.GetModAsync(modId);
        return rv.Data;
    }


    public async Task<FileModel> GetModFileAsync(uint modId, uint fileId)
    {
        var rv = await client.GetModFileAsync(modId, fileId);
        return rv.Data;
    }

    public async Task<ArrayResponse<FileModel>> GetModFilesAsync(
        uint modId,
        string? gameVersion,
        ModLoaderTypeModel? modLoader,
        int index = 0,
        int count = 50)
    {
        // TODO: 改造成能分页拉取直到满足 count 或无剩余
        var rv = await client.GetModFilesAsync(modId, gameVersion, modLoader, (uint)index, (uint)count);
        return rv;
    }
}