using DotNext;
using Polymerium.Trident.Repositories;
using Trident.Abstractions.Errors;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Services;

public class RepositoryService(IEnumerable<IRepository> repositories)
{
    private static readonly DummyRepository DUMMY = new();
    public IEnumerable<IRepository> Repositories => repositories;

    private IRepository OfRepository(string label)
    {
        return repositories.FirstOrDefault(x => x.Label == label) ?? DUMMY;
    }

    public async Task<IEnumerable<Exhibit>> SearchAsync(string label, string query, uint page, uint limit,
        Filter filter, CancellationToken token = default)
    {
        return await OfRepository(label).SearchAsync(query, page, limit, filter, token);
    }


    public async Task<Result<Project, ResourceError>> QueryAsync(string label, string projectId,
        CancellationToken token = default)
    {
        return await OfRepository(label).QueryAsync(projectId, token);
    }

    public async Task<Result<Package, ResourceError>> ResolveAsync(string label, string projectId,
        string? versionId, Filter filter, CancellationToken token = default)
    {
        return await OfRepository(label).ResolveAsync(projectId, versionId, filter, token);
    }
}