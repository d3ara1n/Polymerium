using Microsoft.Extensions.Caching.Memory;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        ResourceType.Modpack | ResourceType.Mod | ResourceType.ResourcePack | ResourceType.ShaderPack;

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
                    Tags = x.Categories.Select(x => x.Name),
                    CreatedAt = x.DateCreated,
                    UpdatedAt = x.DateModified,
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
}
