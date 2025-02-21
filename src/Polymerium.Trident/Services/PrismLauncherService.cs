using Polymerium.Trident.Clients;
using Polymerium.Trident.Models.PrismLauncherApi;

namespace Polymerium.Trident.Services;

// TODO: 日后换成 PlatformService，先偷 PrismLauncher 凑活着
public class PrismLauncherService(IPrismLauncherClient client)
{
    public const string ENDPOINT = "https://meta.prismlauncher.org";
    public const string UID_MINECRAFT = "net.minecraft";
    public const string UID_FORGE = "net.minecraftforge";
    public const string UID_NEOFORGE = "net.neoforged";
    public const string UID_INTERMEDIARY = "net.fabricmc.intermediary";
    public const string UID_FABRIC = "net.fabricmc.fabric-loader";
    public const string UID_QUILT = "org.quiltmc.quilt-loader";

    public async Task<ComponentIndex> GetGameVersionsAsync()
    {
        var index = await client.GetComponentIndexAsync(UID_MINECRAFT);
        return index;
    }
}