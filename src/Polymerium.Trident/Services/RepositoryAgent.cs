using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.Trident.Services;

public class RepositoryAgent
{
    private readonly IReadOnlyDictionary<string, IRepository> _repositories;

    public RepositoryAgent(IEnumerable<IRepository> repositories)
    {
        _repositories = repositories.ToDictionary(repository => repository.Label);
    }

    public int Count => _repositories.Count;
    public IEnumerable<string> Labels => _repositories.Keys;

    private IRepository Redirect(string label)
    {
        if (_repositories.TryGetValue(label, out var repository))
            return repository;

        throw new KeyNotFoundException($"{label} is not a listed repository label or not found");
    }

    public Task<RepositoryStatus> CheckStatusAsync(string label)
    {
        return Redirect(label).CheckStatusAsync();
    }

    public Task<IPaginationHandle<Exhibit>> SearchAsync(string label, string query, Filter filter)
    {
        return Redirect(label).SearchAsync(query, filter);
    }

    public Task<Package> ResolveAsync(string label, string? ns, string pid, string? vid, Filter filter)
    {
        return Redirect(label).ResolveAsync(ns, pid, vid, filter);
    }
}