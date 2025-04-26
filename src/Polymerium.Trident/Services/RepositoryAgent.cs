using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Polymerium.Trident.Services;

public class RepositoryAgent
{
    private static readonly TimeSpan EXPIRED_IN = TimeSpan.FromDays(7);

    public RepositoryAgent(
        IEnumerable<IRepository> repositories,
        ILogger<RepositoryAgent> logger,
        IDistributedCache cache,
        IHttpClientFactory clientFactory)
    {
        _logger = logger;
        _cache = cache;
        _clientFactory = clientFactory;
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

    public Task<RepositoryStatus> CheckStatusAsync(string label) =>
        RetrieveCachedAsync($"status:{label}", () => Redirect(label).CheckStatusAsync());

    public Task<IPaginationHandle<Exhibit>> SearchAsync(string label, string query, Filter filter) =>
        Redirect(label).SearchAsync(query, filter);

    public Task<Package> ResolveAsync(string label, string? ns, string pid, string? vid, Filter filter) =>
        RetrieveCachedAsync($"package:{PackageHelper.Identify(label, ns, pid, vid, filter)}",
                            () => Redirect(label).ResolveAsync(ns, pid, vid, filter));


    public Task<Project> QueryAsync(string label, string? ns, string pid) =>
        RetrieveCachedAsync($"project:{PackageHelper.Identify(label, ns, pid, null, null)}",
                            () => Redirect(label).QueryAsync(ns, pid));

    public Task<IPaginationHandle<Version>> InspectAsync(string label, string? ns, string pid, Filter filter) =>
        Redirect(label).InspectAsync(ns, pid, filter);

    public Task<byte[]> SeeAsync(Uri url) =>
        RetrieveCachedAsync($"thumbnail:{url}",
                            async () =>
                            {
                                using var client = _clientFactory.CreateClient();
                                return await client.GetByteArrayAsync(url).ConfigureAwait(false);
                            });

    private async Task<T> RetrieveCachedAsync<T>(string key, Func<Task<T>> factory)
    {
        var cachedJson = await _cache.GetStringAsync(key).ConfigureAwait(false);
        if (cachedJson != null)
            try
            {
                var cached = JsonSerializer.Deserialize<T>(cachedJson);
                if (cached != null)
                {
                    _logger.LogDebug("Cache hit: {}", key);
                    // await _cache.RefreshAsync(key).ConfigureAwait(false);
                    // NOTE: 不刷新！过期就让他过期，因为是由时效性的
                    //  刷新！因为当前包的解析版本落后无伤大雅，而版本列表是无持久缓存的，不会导致时效性问题
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
            var result = await factory().ConfigureAwait(false);
            await _cache
                 .SetStringAsync(key,
                                 JsonSerializer.Serialize(result),
                                 new DistributedCacheEntryOptions { SlidingExpiration = EXPIRED_IN })
                 .ConfigureAwait(false);
            _logger.LogDebug("Cache missed but recorded: {}", key);


            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception occurred: {message}", e.Message);
            throw;
        }
    }

    private async Task<byte[]> RetrieveCachedAsync(string key, Func<Task<byte[]>> factory)
    {
        var cachedBytes = await _cache.GetAsync(key).ConfigureAwait(false);
        if (cachedBytes != null)
        {
            _logger.LogDebug("Cache bytes hit: {}", key);
            await _cache.RefreshAsync(key).ConfigureAwait(false);
            // 图片而已，过时了不重要，还是可以 Renew 一下的
            return cachedBytes;
        }

        try
        {
            var result = await factory().ConfigureAwait(false);
            await _cache
                 .SetAsync(key, result, new DistributedCacheEntryOptions { SlidingExpiration = EXPIRED_IN })
                 .ConfigureAwait(false);
            _logger.LogDebug("Cache bytes missed but recorded: {}", key);


            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception occurred: {message}", e.Message);
            throw;
        }
    }

    #region Injected

    private readonly IDistributedCache _cache;
    private readonly ILogger<RepositoryAgent> _logger;
    private readonly IReadOnlyDictionary<string, IRepository> _repositories;
    private readonly IHttpClientFactory _clientFactory;

    #endregion
}