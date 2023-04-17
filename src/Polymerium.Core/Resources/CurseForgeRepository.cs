using System;
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
    private readonly IMemoryCache _cache;

    public CurseForgeRepository(IMemoryCache cache)
    {
        _cache = cache;
    }

    public RepositoryLabel Label => RepositoryLabel.CurseForge;
    public ResourceType SupportedResources =>
        ResourceType.Modpack | ResourceType.Mod | ResourceType.ResourcePack;

    public async Task<IEnumerable<RepositoryAssetMeta>> SearchProjectsAsync(
        string query,
        ResourceType type,
        string? modLoader,
        string? version,
        uint offset = 0,
        uint limit = 10,
        CancellationToken token = default
    )
    {
        var results = await CurseForgeHelper.SearchProjectsAsync(
            _cache,
            query,
            type,
            version,
            modLoader,
            offset,
            limit,
            token
        );
        return results.Select(
            x =>
                new RepositoryAssetMeta
                {
                    Repository = RepositoryLabel.CurseForge,
                    Id = x.Id.ToString(),
                    Name = x.Name,
                    Author = string.Join(", ", x.Authors.Select(y => y.Name)),
                    IconSource = x.Logo?.ThumbnailUrl,
                    Summary = x.Summary,
                    Downloads = x.DownloadCount,
                    Type = type,
                    Versions = x.LatestFilesIndexes.Select(x => x.FileId.ToString()),
                    Description = new Lazy<string>(() =>
                    {
                        var description = CurseForgeHelper
                            .GetModDescriptionAsync(x.Id, _cache, token)
                            .Result;
                        return description ?? string.Empty;
                    }),
                    Screenshots = new Lazy<IEnumerable<(string, Uri)>>(
                        x.Screenshots.Select(y => (y.Title, y.Url))
                    )
                }
        );
    }

    public async Task<RepositoryAssetMeta?> GetModAsync(
        string id,
        CancellationToken token = default
    )
    {
        if (uint.TryParse(id, out var projectId))
        {
            var project = await CurseForgeHelper.GetModInfoAsync(projectId, _cache, token);
            if (project.HasValue)
            {
                var result = new RepositoryAssetMeta
                {
                    Repository = RepositoryLabel.CurseForge,
                    Id = id,
                    Name = project.Value.Name,
                    Author = string.Join(", ", project.Value.Authors.Select(x => x.Name)),
                    Summary = project.Value.Summary,
                    IconSource = project.Value.Logo?.ThumbnailUrl,
                    Type = ResourceType.Mod,
                    Downloads = project.Value.DownloadCount,
                    Versions = project.Value.LatestFilesIndexes.Select(x => x.FileId.ToString()),
                    Description = new Lazy<string>(() =>
                    {
                        var description = CurseForgeHelper
                            .GetModDescriptionAsync(project.Value.Id, _cache, token)
                            .Result;
                        return description ?? string.Empty;
                    }),
                    Screenshots = new Lazy<IEnumerable<(string, Uri)>>(
                        project.Value.Screenshots.Select(x => (x.Title, x.Url))
                    )
                };
                return result;
            }
        }

        return null;
    }
}
