using System;
using System.Linq;
using System.Threading.Tasks;
using DotNext;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.ResourceResolving.Attributes;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Helpers;

namespace Polymerium.Core.ResourceResolving;

[ResourceDomain("curseforge")]
public class CurseForgeResolver : ResourceResolverBase
{
    private static readonly Func<string, uint> PARSER_INT = uint.Parse;

    [ResourceType(ResourceType.Mod)]
    [ResourceExpression("{projectId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetModAsync(string projectId, string version)
    {
        var pid = PARSER_INT.TryInvoke(projectId);
        // version 其实就是 fileId
        var fid = PARSER_INT.TryInvoke(version);
        if (pid.IsSuccessful && fid.IsSuccessful)
        {
            var modOption = await CurseForgeHelper.GetModInfoAsync(pid.Value);
            var fileOption = await CurseForgeHelper.GetModFileInfoAsync(pid.Value, fid.Value);
            if (modOption.TryUnwrap(out var eternalMod) && fileOption.TryUnwrap(out var eternalFile))
            {
                var mod = new Mod(eternalMod.Id.ToString(), eternalMod.Name,
                    string.Join(", ", eternalMod.Authors.Select(x => x.Name)),
                    eternalMod.Logo?.ThumbnailUrl, eternalMod.Summary, version,
                    new Uri($"poly-res://curseforge@file/{projectId}/{version}"));
                return Ok(mod, ResourceType.Mod);
            }

            return Err(ResolveResultError.NotFound);
        }

        return Err(ResolveResultError.InvalidArguments);
    }

    [ResourceType(ResourceType.File)]
    [ResourceExpression("{projectId}/{fileId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetFileAsync(string projectId, string fileId)
    {
        var pid = PARSER_INT.TryInvoke(projectId);
        // version 其实就是 fileId
        var fid = PARSER_INT.TryInvoke(fileId);
        if (pid.IsSuccessful && fid.IsSuccessful)
        {
            var fileOption = await CurseForgeHelper.GetModFileInfoAsync(pid.Value, fid.Value);
            if (fileOption.TryUnwrap(out var eternalFile))
            {
                var hashes = eternalFile.Hashes.Where(x => x.Algo == 2);
                string? sha1 = null;
                if (hashes.Any()) sha1 = hashes.First().Value;
                var file = new File(eternalFile.Id.ToString(), eternalFile.DisplayName, string.Empty, null,
                    string.Empty, fileId, $"mods/{eternalFile.FileName}", sha1,
                    eternalFile.DownloadUrl ??
                    new Uri(
                        $"https://edge.forgecdn.net/files/{eternalFile.Id / 1000}/{eternalFile.Id % 1000}/{eternalFile.FileName}"));
                return Ok(file, ResourceType.File);
            }

            return Err(ResolveResultError.NotFound);
        }

        return Err(ResolveResultError.InvalidArguments);
    }
}