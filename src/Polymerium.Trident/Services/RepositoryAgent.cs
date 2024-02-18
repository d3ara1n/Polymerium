using Microsoft.Extensions.Logging;
using Polymerium.Trident.Repositories;
using System.Text.Json;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Services;

public class RepositoryAgent(
    ILogger<RepositoryAgent> logger,
    IEnumerable<IRepository> repositories,
    TridentContext context)
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
        try
        {
            return await OfRepository(label).SearchAsync(query, page, limit, filter, token);
        }
        catch (Exception e)
        {
            logger.LogError("Exception occurred: {message}", e.Message);
            return Enumerable.Empty<Exhibit>();
        }
    }

    private async Task<T> RetrieveCachedResultAsync<T>(string? path, Func<T, string?>? saveTo,
        Func<CancellationToken, Task<T>> action, TimeSpan expireIn,
        CancellationToken token = default)
    {
        if (path != null && File.Exists(path))
            try
            {
                if (DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < expireIn)
                {
                    var content = await File.ReadAllTextAsync(path, token);
                    var cached = JsonSerializer.Deserialize<T>(content);
                    if (cached != null)
                    {
                        logger.LogDebug("Cache hit: {path}", path);
                        return cached;
                    }

                    logger.LogDebug("Bad cache hit: {path}", path);
                }
                else
                {
                    logger.LogDebug("Expired cache hit: {path}", path);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Broken cache hit: {path}", path);
            }

        try
        {
            var result = await action(token);
            var save = path ?? saveTo?.Invoke(result);
            if (save != null)
            {
                var content = JsonSerializer.Serialize(result);
                var dir = Path.GetDirectoryName(save);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir!);
                await File.WriteAllTextAsync(save, content, token);
                logger.LogDebug("Cache missed but recorded: {path}", save);
            }
            else
            {
                logger.LogDebug("Cache missed: {obj}", result!.GetType().Name);
            }

            return result;
        }
        catch (Exception e)
        {
            logger.LogError("Exception occurred: {message}", e.Message);
            throw;
        }
    }


    public async Task<Project> QueryAsync(string label, string projectId,
        CancellationToken token = default)
    {
        return await RetrieveCachedResultAsync(Path.Combine(context.MetadataDir, label, $"{projectId}.json"), null,
            async t => await OfRepository(label).QueryAsync(projectId, t), TimeSpan.FromDays(1), token);
    }

    public async Task<Package> ResolveAsync(string label, string projectId,
        string? versionId, Filter filter, CancellationToken token = default)
    {
        return await RetrieveCachedResultAsync(
            versionId != null ? Path.Combine(context.MetadataDir, label, projectId, $"{versionId}.json") : null,
            r => Path.Combine(context.MetadataDir, label, r.ProjectId, $"{r.VersionId}.json"),
            async t => await OfRepository(label).ResolveAsync(projectId, versionId, filter, t),
            TimeSpan.FromDays(15), token);
    }
}