using System;
using System.Linq;
using System.Threading.Tasks;
using DotNext;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.ResourceResolving.Attributes;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Helpers;
using Polymerium.Core.Models.CurseForge.Eternal;

namespace Polymerium.Core.ResourceResolving;

[ResourceDomain("curseforge")]
public class CurseForgeResolver : ResourceResolverBase
{
    private static readonly Func<string, uint> PARSER_INT = uint.Parse;

    private async Task<Result<ResolveResult, ResolveResultError>> GetProjectAsync(ResourceType type, string projectId,
        string version, Func<EternalProject, EternalModFile, ResourceBase> cast)
    {
        var pid = PARSER_INT.TryInvoke(projectId);
        // version 其实就是 fileId
        var fid = PARSER_INT.TryInvoke(version);
        if (pid.IsSuccessful && fid.IsSuccessful)
        {
            var modOption = await CurseForgeHelper.GetModInfoAsync(pid.Value);
            var fileOption = await CurseForgeHelper.GetModFileInfoAsync(pid.Value, fid.Value);
            if (modOption.TryUnwrap(out var eternalProject) && fileOption.TryUnwrap(out var eternalFile))
            {
                return new ResolveResult(cast(eternalProject, eternalFile), type);
            }

            return Err(ResolveResultError.NotFound);
        }

        return Err(ResolveResultError.InvalidArguments);
    }

    [ResourceType(ResourceType.Modpack)]
    [ResourceExpression("{projectId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetModpackAsync(string projectId, string version)
    {
        return await GetProjectAsync(ResourceType.Modpack, projectId, version,
            (project, _) => new Modpack(project.Id.ToString(), project.Name,
                string.Join(", ", project.Authors.Select(x => x.Name)), project.Logo?.ThumbnailUrl, project.Summary,
                version, new Uri($"poly-res://curseforge@modpack/{projectId}?version={version}")));
    }

    [ResourceType(ResourceType.Mod)]
    [ResourceExpression("{projectId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetModAsync(string projectId, string version)
    {
        return await GetProjectAsync(ResourceType.Mod, projectId, version,
            (project, _) => new Mod(project.Id.ToString(), project.Name,
                string.Join(", ", project.Authors.Select(x => x.Name)), project.Logo?.ThumbnailUrl, project.Summary,
                version, new Uri($"poly-res://curseforge@mod/{projectId}?version={version}")));
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