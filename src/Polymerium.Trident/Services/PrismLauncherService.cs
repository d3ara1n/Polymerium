using Polymerium.Trident.Clients;
using Polymerium.Trident.Engines.Deploying;
using Polymerium.Trident.Models.PrismLauncherApi;
using Polymerium.Trident.Utilities;

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

    public static readonly string OsNameString = PlatformHelper.GetOsName();
    private static readonly string OsFullString = $"{OsNameString}-{PlatformHelper.GetOsArch()}";

    public async Task<ComponentIndex> GetVersionsAsync(string uid, CancellationToken token)
    {
        var index = await client.GetComponentIndexAsync(uid, token);
        return index;
    }

    public Task<ComponentIndex> GetGameVersionsAsync(CancellationToken token) => GetVersionsAsync(UID_MINECRAFT, token);

    public async Task<Component> GetVersionAsync(string uid, string version, CancellationToken token)
    {
        var component = await client.GetComponentAsync(uid, version, token);
        return component;
    }

    public static bool ValidateLibraryRule(Component.Library lib)
    {
        if (lib.Rules != null && lib.Rules.Any())
        {
            return lib.Rules.Any(y =>
            {
                var pass = true;
                // name
                if (y.Os != null && y.Os.TryGetValue("name", out var os))
                {
                    pass = OsFullString == os || OsNameString == os;
                }
                // arch
                // ignore

                return y.Action == "allow" ? pass : !pass;
            });
        }

        return true;
    }

    public static void AddValidatedLibrariesToArtifact(
        DataLockBuilder builder,
        IEnumerable<Component.Library> libraries)
    {
        foreach (var lib in libraries.Where(ValidateLibraryRule))
        {
            if (lib.Url != null)
            {
                // old fashion
                builder.AddLibraryPrismFlavor(lib.Name, lib.Url);
            }
            else if (lib.Downloads.Artifact != null)
            {
                builder.AddLibrary(lib.Name, lib.Downloads.Artifact.Url, lib.Downloads.Artifact.Sha1);
            }

            if (lib.Natives is { Windows: not null })
            {
                var classifier =
                    lib.Natives.Windows.Replace("${arch}", Environment.Is64BitOperatingSystem ? "64" : "32");
                if (lib.Downloads.Classifiers.TryGetValue(classifier, out var download))
                {
                    // NOTE: 假设 native 库本身没有 platform 字段，这是个大胆的假设！
                    builder.AddLibrary($"{lib.Name}:{classifier}", download.Url, download.Sha1, true, false);
                }
            }
        }
    }
}