using DotNext;
using Trident.Abstractions.Errors;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Repositories;

public class DummyRepository : IRepository
{
    public string Label => "_";

    public Task<Result<Project, ResourceError>> QueryAsync(string projectId, CancellationToken token)
    {
        return Task.FromResult(new Result<Project, ResourceError>(ResourceError.NotFound));
    }


    public Task<Result<Package, ResourceError>> ResolveAsync(string projectId, string versionId, CancellationToken token)
    {
        return Task.FromResult(new Result<Package, ResourceError>(ResourceError.NotFound));
    }


    public Task<IEnumerable<Exhibit>> SearchAsync(string keyword, uint page, uint limit, Filter filter,
        CancellationToken token)
    {
        return Task.FromResult(Enumerable.Empty<Exhibit>());
    }
}