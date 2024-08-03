﻿using Polymerium.Trident.Models.PrismLauncher;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Trident.Abstractions.Building;
using Trident.Abstractions.Exceptions;
using Trident.Abstractions.Extensions;

namespace Polymerium.Trident.Helpers;

public static class PrismLauncherHelper
{
    private const string INDEX_URL = "https://meta.prismlauncher.org/v1/{uid}/index.json";
    private const string VERSION_URL = "https://meta.prismlauncher.org/v1/{uid}/{version}.json";

    public const string UID_MINECRAFT = "net.minecraft";
    public const string UID_FORGE = "net.minecraftforge";
    public const string UID_NEOFORGE = "net.neoforged";
    public const string UID_INTERMEDIARY = "net.fabricmc.intermediary";
    public const string UID_FABRIC = "net.fabricmc.fabric-loader";
    public const string UID_QUILT = "org.quiltmc.quilt-loader";

    private static readonly string OsNameString = PlatformHelper.GetOsName();
    private static readonly string OsFullString = $"{OsNameString}-{PlatformHelper.GetOsArch()}";

    private static readonly JsonSerializerOptions OPTIONS = new(JsonSerializerDefaults.Web);

    static PrismLauncherHelper() => OPTIONS.Converters.Add(new JsonStringEnumConverter());

    public static bool ValidateLibraryRule(PrismVersionLibrary lib)
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

                return y.Action == PrismVersionLibraryRuleAction.Allow ? pass : !pass;
            });
        }

        return true;
    }

    public static void AddValidatedLibrariesToArtifact(ArtifactBuilder builder,
        IEnumerable<PrismVersionLibrary> libraries)
    {
        foreach (var lib in libraries.Where(ValidateLibraryRule))
        {
            if (lib.Url != null)
            {
                // old fashion
                builder.AddLibraryPrismFlavor(lib.Name, lib.Url);
            }
            else if (lib.Downloads.Artifact.HasValue)
            {
                builder.AddLibrary(lib.Name, lib.Downloads.Artifact.Value.Url, lib.Downloads.Artifact.Value.Sha1);
            }

            if (lib.Natives.HasValue && lib.Natives.Value.Windows != null)
            {
                var classifier = lib.Natives.Value.Windows.Replace(
                    "${arch}",
                    Environment.Is64BitOperatingSystem ? "64" : "32"
                );
                if (lib.Downloads.Classifiers.TryGetValue(classifier, out var download))
                {
                    // NOTE: 假设 native 库本身没有 platform 字段，这是个大胆的假设！
                    builder.AddLibrary($"{lib.Name}:{classifier}", download.Url, download.Sha1, true, false);
                }
            }
        }
    }

    public static async Task<PrismIndex> GetManifestAsync(string uid, IHttpClientFactory factory,
        CancellationToken token = default)
    {
        using var client = factory.CreateClient();
        var manifest =
            await client.GetFromJsonAsync<PrismIndex>(INDEX_URL.Replace("{uid}", uid), OPTIONS, token);
        if (manifest.Equals(default))
        {
            throw new BadFormatException($"File({INDEX_URL}) failed to download or parse");
        }

        return manifest;
    }

    public static async Task<PrismVersion> GetVersionAsync(string uid, string version, IHttpClientFactory factory,
        CancellationToken token = default)
    {
        using var client = factory.CreateClient();
        var url = VERSION_URL.Replace("{uid}", uid).Replace("{version}", version);
        var index = await client.GetFromJsonAsync<PrismVersion>(url, OPTIONS, token);
        if (index.Equals(default))
        {
            throw new BadFormatException($"File({url}) failed to download or parse");
        }

        return index;
    }

    public static async Task<IEnumerable<PrismVersionLibrary>> GetPatchedLibraries(PrismVersion version,
        IHttpClientFactory factory, CancellationToken token = default)
    {
        List<PrismVersionLibrary> libraries = new(version.Libraries ?? Enumerable.Empty<PrismVersionLibrary>());
        using var client = factory.CreateClient();
        foreach (var requirement in version.Requires)
        {
            var url = VERSION_URL.Replace("{uid}", requirement.Uid).Replace("{version}",
                requirement.Suggest ?? requirement.Equal ?? throw new BadFormatException($"{version.Uid}.json",
                    $"requires[{requirement.Uid}].equals|suggests"));
            var sub = await client.GetFromJsonAsync<PrismVersion>(url, OPTIONS, token);
            foreach (var lib in sub.Libraries ?? Enumerable.Empty<PrismVersionLibrary>())
            {
                libraries.Add(lib);
            }
        }

        return libraries;
    }
}