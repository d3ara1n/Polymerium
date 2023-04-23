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
                    Type = type,
                    Versions = x.Versions,
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

    public async Task<RepositoryAssetMeta?> GetModAsync(
        string id,
        CancellationToken token = default
    )
    {
        var project = await ModrinthHelper.GetProjectAsync(id, _cache, token);
        if (project.HasValue)
        {
            var result = new RepositoryAssetMeta
            {
                Repository = RepositoryLabel.Modrinth,
                Id = id,
                Name = project.Value.Title,
                Author = project.Value.Team,
                IconSource = project.Value.IconUrl,
                Downloads = project.Value.Downloads,
                Summary = project.Value.Description,
                Versions = project.Value.Versions,
                Type = ResourceType.Mod,
                Screenshots = new Lazy<IEnumerable<(string, Uri)>>(
                    project.Value.Gallery.Select(x => (x.Title, x.Url))
                ),
                Description = new Lazy<string>(project.Value.Body)
            };
            return result;
        }

        return null;
    }
}
