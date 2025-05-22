using Polymerium.Trident.Services;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Polymerium.Trident.Repositories;

public class ModrinthRepository : IRepository
{
    public string Label => ModrinthService.LABEL;

    public Task<RepositoryStatus> CheckStatusAsync() => throw new NotImplementedException();

    public Task<IPaginationHandle<Exhibit>> SearchAsync(string query, Filter filter) =>
        throw new NotImplementedException();

    public Task<Project> QueryAsync(string? ns, string pid) => throw new NotImplementedException();

    public Task<IEnumerable<Project>> QueryBatchAsync(IEnumerable<(string?, string pid)> batch) =>
        throw new NotImplementedException();

    public Task<Package> ResolveAsync(string? ns, string pid, string? vid, Filter filter) =>
        throw new NotImplementedException();

    public Task<string> ReadDescriptionAsync(string? ns, string pid) => throw new NotImplementedException();

    public Task<string> ReadChangelogAsync(string? ns, string pid, string vid) => throw new NotImplementedException();

    public Task<IPaginationHandle<Version>> InspectAsync(string? ns, string pid, Filter filter) =>
        throw new NotImplementedException();
}