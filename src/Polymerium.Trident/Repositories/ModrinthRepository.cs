using Microsoft.Extensions.Logging;
using Polymerium.Trident.Helpers;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Repositories;

public class ModrinthRepository(ILogger<ModrinthRepository> logger, IHttpClientFactory factory) : IRepository
{
    public string Label => RepositoryLabels.MODRINTH;

    public async Task<Project> QueryAsync(string projectId, CancellationToken token) =>
        await ModrinthHelper.GetIntoProjectAsync(logger, factory, projectId, token);

    public async Task<Package> ResolveAsync(string projectId, string? versionId, Filter filter,
        CancellationToken token) =>
        await ModrinthHelper.GetIntoPackageAsync(logger, factory, projectId, versionId, filter.Version,
            filter.ModLoader, token);

    public async Task<IEnumerable<Exhibit>> SearchAsync(string keyword, uint page, uint limit, Filter filter,
        CancellationToken token)
    {
        var kind = filter.Kind ?? ResourceKind.Modpack;
        return (await ModrinthHelper.SearchProjectsAsync(logger, factory, keyword, kind,
                filter.Version, filter.ModLoader, page, limit, token))
            .Select(x => new Exhibit(x.ProjectId, x.Title, RepositoryLabels.MODRINTH,
                x.IconUrl,
                kind,
                x.Author, x.Description, x.DateCreated, x.DateModified,
                x.Downloads));
    }
}