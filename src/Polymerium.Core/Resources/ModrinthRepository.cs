using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Markdig;
using Microsoft.Extensions.Caching.Memory;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Helpers;

namespace Polymerium.Core.Resources;

public class ModrinthRepository : IResourceRepository
{
    private readonly IMemoryCache _cache;

    public ModrinthRepository(IMemoryCache cache)
    {
        _cache = cache;
    }

    public RepositoryLabel Label => RepositoryLabel.Modrinth;

    public ResourceType SupportedResources =>
        ResourceType.Modpack
        | ResourceType.Mod
        | ResourceType.ShaderPack
        | ResourceType.ResourcePack;

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
        var results = await ModrinthHelper.SearchProjectsAsync(
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
                    Repository = RepositoryLabel.Modrinth,
                    Id = x.ProjectId,
                    Name = x.Title,
                    Author = x.Author,
                    IconSource = x.IconUrl,
                    Summary = x.Description,
                    Downloads = x.Downloads,
                    CreatedAt = x.DateCreated,
                    UpdatedAt = x.DateModified,
                    Tags = x.Categories,
                    Type = type,
                    Description = new Lazy<string>(() =>
                    {
                        var project = ModrinthHelper
                            .GetProjectAsync(x.ProjectId, _cache, token)
                            .Result;
                        if (project.HasValue)
                            return Markdown.ToHtml(project.Value.Body);
                        return string.Empty;
                    }),
                    Screenshots = new Lazy<IEnumerable<(string, Uri)>>(
                        x.Gallery.Select(y => (string.Empty, y))
                    )
                }
        );
    }
}