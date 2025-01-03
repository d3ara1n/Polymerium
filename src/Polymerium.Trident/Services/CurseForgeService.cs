using Polymerium.Trident.Clients;
using Polymerium.Trident.Models.CurseForgeApi;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;

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

    public ModLoaderTypeModel? LoaderIdToType(string? loader)
    {
        return loader switch
        {
            LoaderHelper.LOADERID_NEOFORGE => ModLoaderTypeModel.NeoForge,
            LoaderHelper.LOADERID_FORGE => ModLoaderTypeModel.Forge,
            LoaderHelper.LOADERID_FABRIC => ModLoaderTypeModel.Fabric,
            LoaderHelper.LOADERID_QUILT => ModLoaderTypeModel.Quilt,
            _ => null
        };
    }

    public uint? ResourceKindToClassId(ResourceKind? kind)
    {
        return kind switch
        {
            ResourceKind.Modpack => CLASSID_MODPACK,
            ResourceKind.Mod => CLASSID_MOD,
            ResourceKind.ResourcePack => CLASSID_RESOURCEPACK,
            ResourceKind.ShaderPack => CLASSID_SHADERPACK,
            _ => null
        };
    }


    public Exhibit ModModelToExhibit(ModModel model)
    {
        return new Exhibit(LABEL, null, model.Id.ToString(), model.Name, model.Logo.ThumbnailUrl,
            string.Join(',', model.Authors.Select(x => x.Name)), model.Summary, model.DownloadCount, model.DateCreated,
            model.DateModified);
    }

    public async Task<IReadOnlyList<string>> GetGameVersionsAsync()
    {
        var types = (await client.GetVersionTypes()).Data
            .Where(x => x.Status == 1 && x.Name.StartsWith("Minecraft"))
            .Select(x => x.Id);
        var res = await client.GetVersions();
        var versions = res.Data.Where(x => types.Contains(x.Type)).SelectMany(x => x.Versions).ToList();
        return versions;
    }

    public async Task<SearchResponse<ModModel>> SearchAsync(string searchFilter, uint? classId,
        string? gameVersion, ModLoaderTypeModel? modLoader, uint index = 0, uint pageSize = 50)
    {
        return await client.SearchMods(searchFilter, classId, gameVersion, modLoader, index, pageSize);
    }
}