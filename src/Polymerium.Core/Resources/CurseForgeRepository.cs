using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Helpers;

namespace Polymerium.Core.Resources;

public class CurseForgeRepository : IResourceRepository
{
    public RepositoryLabel Label => RepositoryLabel.CurseForge;
    public ResourceType SupportedResources => ResourceType.Modpack | ResourceType.Mod | ResourceType.ResourcePack;

    private readonly IMemoryCache _cache;

    public CurseForgeRepository(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<IEnumerable<RepositoryAssetMeta>> SearchProjectsAsync(string query, ResourceType type,
        string? modLoader, string? version, uint offset = 0, uint limit = 10, CancellationToken token = default)
    {
        var results =
            await CurseForgeHelper.SearchProjectsAsync(_cache, query, type, version, modLoader, offset, limit, token);
        return results.Select(x => new RepositoryAssetMeta
        {
            Repository = RepositoryLabel.CurseForge,
            Id = x.Id.ToString(),
            Name = x.Name,
            Author = string.Join(", ", x.Authors.Select(y => y.Name)),
            IconSource = x.Logo?.ThumbnailUrl,
            Summary = x.Summary,
            Type = type,
            Versions = x.LatestFilesIndexes.Select(x => x.FileId.ToString())
        });
    }

    public async Task<RepositoryAssetMeta?> GetModAsync(string id, CancellationToken token = default)
    {
        if (uint.TryParse(id, out var projectId))
        {
            var option = await CurseForgeHelper.GetModInfoAsync(projectId, _cache, token);
            if (option.TryUnwrap(out var project))
            {
                var result = new RepositoryAssetMeta
                {
                    Repository = RepositoryLabel.CurseForge,
                    Id = id,
                    Name = project.Name,
                    Author = string.Join(", ", project.Authors.Select(x => x.Name)),
                    Summary = project.Summary,
                    IconSource = project.Logo?.ThumbnailUrl,
                    Type = ResourceType.Mod,
                    Versions = project.LatestFilesIndexes.Select(x => x.FileId.ToString())
                };
                return result;
            }
        }

        return null;
    }
}