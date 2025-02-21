using Polymerium.Trident.Clients;

namespace Polymerium.Trident.Services;

public class PrismLauncherService(IPrismLauncherClient client)
{
    public const string ENDPOINT = "https://meta.prismlauncher.org";
    public const string UID_MINECRAFT = "net.minecraft";
    public const string UID_FORGE = "net.minecraftforge";
    public const string UID_NEOFORGE = "net.neoforged";
    public const string UID_INTERMEDIARY = "net.fabricmc.intermediary";
    public const string UID_FABRIC = "net.fabricmc.fabric-loader";
    public const string UID_QUILT = "org.quiltmc.quilt-loader";
}