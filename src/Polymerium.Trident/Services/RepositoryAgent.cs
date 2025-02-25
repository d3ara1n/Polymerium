using System.Text.Json;
using Microsoft.Extensions.Logging;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Polymerium.Trident.Services;

public class RepositoryAgent
{
    private static readonly TimeSpan EXPIRED_IN = TimeSpan.FromDays(7);
    private readonly ILogger<RepositoryAgent> _logger;
    private readonly IReadOnlyDictionary<string, IRepository> _repositories;

    public RepositoryAgent(IEnumerable<IRepository> repositories, ILogger<RepositoryAgent> logger)
    {
        _logger = logger;
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

    public Task<RepositoryStatus> CheckStatusAsync(string label) => Redirect(label).CheckStatusAsync();

    public Task<IPaginationHandle<Exhibit>> SearchAsync(string label, string query, Filter filter) =>
        Redirect(label).SearchAsync(query, filter);

    public Task<Package> ResolveAsync(string label, string? ns, string pid, string? vid, Filter filter) =>
        RetrieveCachedAsync(vid is not null
                                ? Path.Combine(PathDef.Default.CachePackageDirectory, label, pid, $"{vid}.json")
                                : null,
                            r => Path.Combine(PathDef.Default.CachePackageDirectory,
                                              r.Label,
                                              r.ProjectId,
                                              $"{r.VersionId}.json"),
                            () => Redirect(label).ResolveAsync(ns, pid, vid, filter));

    public Task<IPaginationHandle<Version>> InspectAsync(string label, string? ns, string pid, Filter filter) =>
        Redirect(label).InspectAsync(ns, pid, filter);

    private async Task<T> RetrieveCachedAsync<T>(string? cachedPath, Func<T, string>? saveTo, Func<Task<T>> factory)
    {
        if (cachedPath != null && File.Exists(cachedPath))
            try
            {
                if (DateTime.UtcNow - File.GetLastWriteTimeUtc(cachedPath) < EXPIRED_IN)
                {
                    var content = await File.ReadAllTextAsync(cachedPath);
                    var cached = JsonSerializer.Deserialize<T>(content);
                    if (cached != null)
                    {
                        _logger.LogDebug("Cache hit: {path}", cachedPath);
                        return cached;
                    }

                    _logger.LogDebug("Bad cache hit: {path}", cachedPath);
                }
                else
                {
                    _logger.LogDebug("Expired cache hit: {path}", cachedPath);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Broken cache hit: {path}", cachedPath);
            }

        try
        {
            var result = await factory();
            var save = cachedPath ?? saveTo?.Invoke(result);
            if (save != null)
            {
                var content = JsonSerializer.Serialize(result);
                var dir = Path.GetDirectoryName(save);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir!);

                await File.WriteAllTextAsync(save, content);
                _logger.LogDebug("Cache missed but recorded: {path}", save);
            }
            else
            {
                _logger.LogDebug("Cache missed: {obj}", result!.GetType().Name);
            }

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError("Exception occurred: {message}", e.Message);
            throw;
        }
    }
}