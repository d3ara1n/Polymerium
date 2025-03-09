using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Polymerium.Trident.Services;

public class RepositoryAgent
{
    private static readonly TimeSpan EXPIRED_IN = TimeSpan.FromHours(72);
    private readonly ILogger<RepositoryAgent> _logger;
    private readonly IReadOnlyDictionary<string, IRepository> _repositories;
    private readonly IDistributedCache _cache;

    public RepositoryAgent(
        IEnumerable<IRepository> repositories,
        ILogger<RepositoryAgent> logger,
        IDistributedCache cache)
    {
        _logger = logger;
        _cache = cache;
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
        RetrieveCachedAsync(KeyOf("package", label, ns, pid, vid, filter),
                            () => Redirect(label).ResolveAsync(ns, pid, vid, filter));

    public Task<IPaginationHandle<Version>> InspectAsync(string label, string? ns, string pid, Filter filter) =>
        Redirect(label).InspectAsync(ns, pid, filter);

    private async Task<T> RetrieveCachedAsync<T>(string key, Func<Task<T>> factory)
    {
        var cachedJson = await _cache.GetStringAsync(key);
        if (cachedJson != null)
            try
            {
                var cached = JsonSerializer.Deserialize<T>(cachedJson);
                if (cached != null)
                {
                    _logger.LogDebug("Cache hit: {}", key);
                    await _cache.RefreshAsync(key);
                    return cached;
                }

                _logger.LogDebug("Bad cache hit: {}", key);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Broken cache hit: {}", key);
            }

        try
        {
            var result = await factory();
            await _cache.SetStringAsync(key,
                                        JsonSerializer.Serialize(result),
                                        new DistributedCacheEntryOptions { SlidingExpiration = EXPIRED_IN });
            _logger.LogDebug("Cache missed but recorded: {}", key);


            return result;
        }
        catch (Exception e)
        {
            _logger.LogError("Exception occurred: {message}", e.Message);
            throw;
        }
    }

    private string KeyOf(string type, string label, string? ns, string pid, string? vid, Filter filter)
    {
        var sb = new StringBuilder(type);
        sb.Append(':');
        sb.Append(label);
        sb.Append(':');
        if (ns != null)
        {
            sb.Append(ns);
            sb.Append('/');
        }

        sb.Append(pid);
        if (vid != null)
        {
            sb.Append('@');
            sb.Append(vid);
        }
        else
        {
            if (filter.Kind != null)
            {
                sb.Append("#kind=");
                sb.Append(filter.Kind);
            }

            if (filter.Version != null)
            {
                sb.Append("#version=");
                sb.Append(filter.Version);
            }

            if (filter.Loader != null)
            {
                sb.Append("#loader=");
                sb.Append(filter.Loader);
            }
        }

        return sb.ToString();
    }
}