using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Polymerium.Trident.Services;
using Polymerium.Trident.Utilities;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Importers;
using Trident.Abstractions.Utilities;
using Index = Polymerium.Trident.Models.ModrinthPack.Index;

namespace Polymerium.Trident.Importers;

public class ModrinthImporter : IProfileImporter
{
    private static readonly Dictionary<string, string> LOADER_MAPPINGS = new()
    {
        ["forge"] = LoaderHelper.LOADERID_FORGE,
        ["neoforge"] = LoaderHelper.LOADERID_NEOFORGE,
        ["fabric-loader"] = LoaderHelper.LOADERID_FABRIC,
        ["quilt-loader"] = LoaderHelper.LOADERID_QUILT
    };

    #region IProfileImporter Members

    public string IndexFileName => "modrinth.index.json";

    public async Task<ImportedProfileContainer> ExtractAsync(CompressedProfilePack pack)
    {
        await using var manifestStream = pack.Open(IndexFileName);
        var index = await JsonSerializer
                         .DeserializeAsync<Index>(manifestStream, JsonSerializerOptions.Web)
                         .ConfigureAwait(false);
        if (index is null
         || !TryExtractLoader(index.Dependencies, out var loader)
         || !TryExtractVersion(index.Dependencies, out var version))
            throw new FormatException($"{IndexFileName} is not a valid manifest");

        var source = pack.Reference is not null ? PackageHelper.ToPurl(pack.Reference) : null;
        return new(new(index.Name,
                       new(source,
                           version,
                           LoaderHelper.ToLurl(loader.Identity, loader.Version),
                           [.. index.Files.Where(x => x.Env?.Client is not "unsupported").Select(ToPackage)]),
                       new Dictionary<string, object>()),
                   pack
                      .FileNames
                      .Where(x => x.StartsWith("overrides") && x != "overrides" && x.Length > "overrides".Length + 1)
                      .Select(x => (x, x[("overrides".Length + 1)..]))
                      .Where(x => !x.Item2.EndsWith('/') && !ImporterAgent.INVALID_NAMES.Contains(x.Item2))
                      .Concat(pack
                             .FileNames
                             .Where(x => x.StartsWith("client-overrides")
                                      && x != "client-overrides"
                                      && x.Length > "client-overrides".Length + 1)
                             .Select(x => (x, x[("client-overrides".Length + 1)..]))
                             .Where(x => !x.Item2.EndsWith('/') && !ImporterAgent.INVALID_NAMES.Contains(x.Item2)))
                      .ToList(),
                   pack.Reference?.Thumbnail);
    }

    #endregion

    private bool TryExtractLoader(
        IDictionary<string, string> dependencies,
        out (string Identity, string Version) loader)
    {
        foreach (var (k, v) in dependencies)
            if (LOADER_MAPPINGS.TryGetValue(k, out var mapping))
            {
                loader = (mapping, v);
                return true;
            }

        loader = default((string, string));
        return false;
    }

    private bool TryExtractVersion(IDictionary<string, string> dependencies, [MaybeNullWhen(false)] out string version)
    {
        if (dependencies.TryGetValue("minecraft", out var v))
        {
            version = v;
            return true;
        }

        version = null;
        return false;
    }

    private Profile.Rice.Entry ToPackage(Index.IndexFile file)
    {
        // FIX: 需要兼容 bbsmc
        //  bbsmc 用的第三方包，其中部分使用 mrpack，而 mrpack 使用多个源，其中就有 forgecdn
        //  也就是 mrpack 可以包含多个托管站
        // FIX: 有些 %版本% 写的是文件名
        var download = file.Downloads.FirstOrDefault(x => x.Host == "cdn.modrinth.com");
        // https://cdn.modrinth.com/data/88888888/versions/88888888/filename.jar
        if (download != null)
        {
            var path = download.AbsolutePath;
            if (path.Length > 32)
            {
                var projectId = path[6..14];
                var versionId = path[24..32];
                return new(PackageHelper.ToPurl("modrinth", null, projectId, versionId), true, null, []);
            }
        }

        // or dead end
        throw new NotSupportedException($"{file.Path} can not be recognized as an attachment");
    }
}
