using Polymerium.Trident.Clients;

namespace Polymerium.Trident.Services;

public class CurseForgeService(ICurseForgeClient client)
{
    public const string NAME = "CurseForge";

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

    public async Task<IReadOnlyList<string>> GetGameVersionsAsync()
    {
        var types = (await client.GetVersionTypes()).Data
            .Where(x => x.Status == 1 && x.Name.StartsWith("Minecraft"))
            .Select(x => x.Id);
        var res = await client.GetVersions();
        var versions = res.Data.Where(x => types.Contains(x.Type)).SelectMany(x => x.Versions).ToList();
        return versions;
    }
}