using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Repositories;

public class DummyRepository : IRepository
{
    public string Label => "_";

    public Task<Project> QueryAsync(string projectId, CancellationToken token) => throw new NotImplementedException();

    public Task<Package> ResolveAsync(string projectId, string? versionId, Filter filter,
        CancellationToken token) =>
        throw new NotImplementedException();

    public Task<IEnumerable<Exhibit>> SearchAsync(string keyword, uint page, uint limit, Filter filter,
        CancellationToken token) =>
        throw new NotImplementedException();
}