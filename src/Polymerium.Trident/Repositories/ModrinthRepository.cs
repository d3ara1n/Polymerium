using DotNext;
using Trident.Abstractions.Errors;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Repositories;

public class ModrinthRepository : IRepository
{
    public string Label => RepositoryLabels.MODRINTH;

    public Task<Result<Project, ResourceError>> QueryAsync(string projectId, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<Result<Package, ResourceError>> ResolveAsync(string projectId, string? versionId, Filter filter,
        CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Exhibit>> SearchAsync(string keyword, uint page, uint limit, Filter filter,
        CancellationToken token)
    {
        throw new NotImplementedException();
    }
}