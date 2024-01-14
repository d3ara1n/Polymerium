using System.Text.Json;
using DotNext;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Repositories;
using Trident.Abstractions.Errors;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Services;

public class RepositoryService(
    ILogger<RepositoryService> logger,
    IEnumerable<IRepository> repositories,
    PolymeriumContext context)
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
        return await OfRepository(label).SearchAsync(query, page, limit, filter, token);
    }

    private async Task<Result<T, ResourceError>> RetrieveCachedResultAsync<T>(string? path, Func<T, string?>? saveTo,
        Func<CancellationToken, Task<Result<T, ResourceError>>> action, TimeSpan expireIn,
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
                        logger.LogInformation("Cache hit: {path}", path);
                        return new Result<T, ResourceError>(cached);
                    }

                    logger.LogInformation("Bad cache hit: {path}", path);
                }
                else
                {
                    logger.LogInformation("Expired cache hit: {path}", path);
                }
            }
            catch (Exception e)
            {
                logger.LogInformation("Broken cache hit: {path} for {message}", path, e.Message);
            }

        try
        {
            var result = await action(token);
            if (result.IsSuccessful)
                try
                {
                    var save = path ?? saveTo?.Invoke(result.Value);
                    if (save != null)
                    {
                        var content = JsonSerializer.Serialize(result.Value);
                        var dir = Path.GetDirectoryName(save);
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir!);
                        await File.WriteAllTextAsync(save, content, token);
                        logger.LogInformation("Cache missed but recorded: {path}", save);
                    }
                    else
                    {
                        logger.LogInformation("Cache missed: {obj}", result.Value);
                    }
                }
                catch (Exception e)
                {
                    logger.LogError("Cache missed and record failed: {obj} for {message}", result.Value, e.Message);
                }
            else
                logger.LogInformation("Cache missed and retrieve failed: {path}", path);

            return result;
        }
        catch (Exception e)
        {
            logger.LogError("Exception occurred: {message}", e.Message);
            return new Result<T, ResourceError>(ResourceError.Unsupported);
        }
    }


    public async Task<Result<Project, ResourceError>> QueryAsync(string label, string projectId,
        CancellationToken token = default)
    {
        return await RetrieveCachedResultAsync(Path.Combine(context.MetadataDir, label, $"{projectId}.json"), null,
            async t => await OfRepository(label).QueryAsync(projectId, t), TimeSpan.FromDays(1), token);
    }

    public async Task<Result<Package, ResourceError>> ResolveAsync(string label, string projectId,
        string? versionId, Filter filter, CancellationToken token = default)
    {
        return await RetrieveCachedResultAsync(
            versionId != null ? Path.Combine(context.MetadataDir, label, projectId, $"{versionId}.json") : null,
            r => Path.Combine(context.MetadataDir, label, r.ProjectId, $"{r.VersionId}.json"),
            async t => await OfRepository(label).ResolveAsync(projectId, versionId, filter, t),
            TimeSpan.FromDays(15), token);
    }
}