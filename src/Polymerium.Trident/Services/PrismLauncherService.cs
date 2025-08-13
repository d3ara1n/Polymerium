using Polymerium.Trident.Clients;
using Polymerium.Trident.Engines.Deploying;
using Polymerium.Trident.Models.PrismLauncherApi;
using Polymerium.Trident.Utilities;
using Trident.Abstractions.Utilities;

namespace Polymerium.Trident.Services
{
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

        public static readonly IReadOnlyDictionary<string, string> UID_MAPPINGS = new Dictionary<string, string>
        {
            [LoaderHelper.LOADERID_FORGE] = UID_FORGE,
            [LoaderHelper.LOADERID_NEOFORGE] = UID_NEOFORGE,
            [LoaderHelper.LOADERID_FABRIC] = UID_FABRIC,
            [LoaderHelper.LOADERID_QUILT] = UID_QUILT
        };

        public async Task<ComponentIndex> GetVersionsAsync(string uid, CancellationToken token)
        {
            var index = await client.GetComponentIndexAsync(uid, token).ConfigureAwait(false);
            return index;
        }

        public async Task<IReadOnlyList<ComponentIndex.ComponentVersion>> GetVersionsForMinecraftVersionAsync(
            string uid,
            string version,
            CancellationToken token)
        {
            var index = await client.GetComponentIndexAsync(uid, token).ConfigureAwait(false);
            return
            [
                .. index.Versions.Where(x => x.Requires.Any(y => y.Uid == UID_INTERMEDIARY
                                                              || (y.Uid == UID_MINECRAFT
                                                               && (y.Suggest == version || y.Equal == version))))
            ];
        }

        public Task<ComponentIndex> GetMinecraftVersionsAsync(CancellationToken token) =>
            GetVersionsAsync(UID_MINECRAFT, token);

        public async Task<Component> GetVersionAsync(string uid, string version, CancellationToken token)
        {
            var component = await client.GetComponentAsync(uid, version, token).ConfigureAwait(false);
            return component;
        }


        public async Task<IEnumerable<Component.Library>> GetPatchedLibraries(
            Component version,
            CancellationToken token)
        {
            var libraries = new List<Component.Library>(version.Libraries ?? Enumerable.Empty<Component.Library>());
            foreach (var requirement in version.Requires)
            {
                var sub = await GetVersionAsync(requirement.Uid,
                                                requirement.Suggest
                                             ?? requirement.Equal
                                             ?? throw new
                                                    FormatException($"{version.Uid}.json/requires[{requirement.Uid}].equals|suggests"),
                                                token)
                             .ConfigureAwait(false);
                libraries.AddRange(sub.Libraries ?? Enumerable.Empty<Component.Library>());
            }

            return libraries;
        }

        public async Task<RuntimeManifest> GetRuntimeAsync(uint major, CancellationToken token)
        {
            var manifest = await client.GetRuntimeAsync(major, token).ConfigureAwait(false);
            return manifest;
        }

        public static bool ValidateLibraryRule(Component.Library lib)
        {
            if (lib.Rules != null && lib.Rules.Any())
            {
                var rv = lib.Rules.All(y =>
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
                return rv;
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
                    // old fashion
                {
                    builder.AddLibraryPrismFlavor(lib.Name, lib.Url);
                }
                else if (lib.Downloads is { Artifact: { } artifact })
                {
                    builder.AddLibrary(lib.Name, artifact.Url, artifact.Sha1);
                }

                if (lib is { Natives.Windows: { } windows, Downloads: { } downloads })
                {
                    var classifier = windows.Replace("${arch}", Environment.Is64BitOperatingSystem ? "64" : "32");
                    if (downloads.Classifiers.TryGetValue(classifier, out var download))
                        // NOTE: 假设 native 库本身没有 platform 字段，这是个大胆的假设！
                    {
                        builder.AddLibrary($"{lib.Name}:{classifier}", download.Url, download.Sha1, true, false);
                    }
                }
            }
        }
    }
}
