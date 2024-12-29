using Trident.Abstractions.Repositories;

namespace Polymerium.Trident.Services;

public class RepositoryAgent
{
    public int Count => _repositories.Count;
    public IEnumerable<string> Labels => _repositories.Keys;

    private readonly IReadOnlyDictionary<string, IRepository> _repositories;

    public RepositoryAgent(IEnumerable<IRepository> repositories)
    {
        _repositories = repositories.ToDictionary(repository => repository.Label);
    }

    public async Task<RepositoryStatus> CheckStatusAsync(string label)
    {
        if (_repositories.TryGetValue(label, out var repository))
            return await repository.CheckStatusAsync();

        throw new KeyNotFoundException($"{label} is not a listed repository label or not found");
    }
}