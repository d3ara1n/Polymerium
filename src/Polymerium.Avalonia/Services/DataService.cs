using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Polymerium.Avalonia.Utilities;
using Microsoft.Extensions.Caching.Memory;
using TridentCore.Abstractions;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Models.MojangLauncherApi;
using TridentCore.Core.Models.PrismLauncherApi;
using TridentCore.Core.Services;
using TridentCore.Core.Utilities;
using TridentCore.Pref;
using Version = TridentCore.Abstractions.Repositories.Resources.Version;

namespace Polymerium.Avalonia.Services;

// Application 级数据整合服务，所有 API/模型统一经此提供。
public class DataService(
    IMemoryCache cache,
    RepositoryAgent agent,
    PrismLauncherService prismLauncherService,
    MojangService mojangService,
    IHttpClientFactory httpClientFactory)
{
    private static readonly TimeSpan EXPIRED_IN = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ICON_FILE_EXPIRED_IN = TimeSpan.FromDays(30);
    private readonly ConcurrentDictionary<string, Lazy<Task<Uri>>> _imageFiles = new(StringComparer.Ordinal);

    public async ValueTask<Package> IdentifyVersionAsync(string filePath) => await agent.IdentifyAsync(filePath);

    // Package/Project/Description/Changelog/Status 缓存归 Trident 仓库缓存层管，此处直接委托；
    // DataService 只缓存 UI hot data 与应用层加工后的数据。
    public Task<Package> ResolvePackageAsync(PackageIdentifier id, Filter filter, bool cachedEnabled = true) =>
        agent.ResolveAsync(id, filter, cachedEnabled);

    public Task<BatchResult<PackageIdentifier, Package>> ResolvePackagesAsync(
        IEnumerable<PackageIdentifier> batch,
        Filter filter) =>
        agent.ResolveBatchAsync(batch, filter);

    public Task<Project> QueryProjectAsync(ProjectIdentifier id) => agent.QueryAsync(id);

    public Task<BatchResult<ProjectIdentifier, Project>> QueryProjectsAsync(
        IEnumerable<ProjectIdentifier> batch) =>
        agent.QueryBatchAsync(batch);

    public Task<string> ReadDescriptionAsync(ProjectIdentifier id) => agent.ReadDescriptionAsync(id);

    public Task<string> ReadChangelogAsync(PackageIdentifier id) => agent.ReadChangelogAsync(id);

    public Task<RepositoryStatus> CheckStatusAsync(string label) => agent.CheckStatusAsync(label);

    public Task<Uri> GetImageFileAsync(Uri url, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var shared = _imageFiles.GetOrAdd(url.AbsoluteUri, _ => new(() => StartImageFileLoad(url))).Value;
        return cancellationToken.CanBeCanceled ? shared.WaitAsync(cancellationToken) : shared;
    }

    private Task<Uri> StartImageFileLoad(Uri url)
    {
        var task = LoadOrDownloadImageFileAsync(url);
        // NOTE: All waiters may cancel while the shared download continues to populate the file cache.
        _ = task.ContinueWith(static failed => { _ = failed.Exception; }, CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return task;
    }

    private async Task<Uri> LoadOrDownloadImageFileAsync(Uri url)
    {
        try
        {
            var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(url.AbsoluteUri))).ToLowerInvariant();
            var path = PathDef.Default.FileOfIconObject(hash);
            var file = new FileInfo(path);
            if (file is { Exists: true, Length: > 0 } && file.LastWriteTimeUtc + ICON_FILE_EXPIRED_IN > DateTime.UtcNow)
            {
                return ImageSourceHelper.FromFile(path);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using var client = httpClientFactory.CreateClient();
                using var timeout = new CancellationTokenSource(client.Timeout);
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                                 81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    await response.Content.CopyToAsync(output, timeout.Token).ConfigureAwait(false);
                    if (output.Length == 0)
                    {
                        throw new InvalidDataException("The image response is empty.");
                    }
                }

                File.Move(temporary, path, true);
                return ImageSourceHelper.FromFile(path);
            }
            finally
            {
                File.Delete(temporary);
            }
        }
        finally
        {
            _imageFiles.TryRemove(url.AbsoluteUri, out _);
        }
    }

    public ValueTask<IEnumerable<Version>> InspectVersionsAsync(string label, string? ns, string pid, Filter filter) =>
        GetOrCreate($"versions:{label}:{PackageHelper.Identify(label, ns, pid, null, filter)}",
                    async () =>
                    {
                        // 调用以读展示数据为主，仅版本匹配需全量；此处设上限避免一次拉取过多。
                        const int limit = 20;
                        var handle = await agent.InspectAsync(new(label, ns, pid), filter);
                        var rv = new List<Version>();
                        int lastCount;
                        var index = 0u;
                        do
                        {
                            lastCount = rv.Count;
                            handle.PageIndex = index;
                            rv.AddRange(await handle.FetchAsync(CancellationToken.None));
                            index++;
                        } while (rv.Count != lastCount && rv.Count < limit);

                        return rv.AsEnumerable();
                    });

    public ValueTask<ComponentIndex> GetComponentAsync(string loaderId) =>
        GetOrCreate($"loader:{loaderId}",
                    () => prismLauncherService.GetVersionsAsync(PrismLauncherService.UidMappings[loaderId],
                                                                CancellationToken.None));

    public ValueTask<IReadOnlyList<ComponentIndex.ComponentVersion>> GetComponentVersionsAsync(
        string loaderId,
        string gameVersion) =>
        GetOrCreate($"loader:{loaderId}:{gameVersion}",
                    () => prismLauncherService.GetVersionsForMinecraftVersionAsync(PrismLauncherService.UidMappings
                            [loaderId],
                        gameVersion,
                        CancellationToken.None));

    public ValueTask<ComponentIndex> GetMinecraftVersionsAsync() =>
        GetOrCreate("minecraft:versions", () => prismLauncherService.GetMinecraftVersionsAsync(CancellationToken.None));

    public ValueTask<MinecraftNewsResponse> GetMinecraftNewsAsync() =>
        GetOrCreate("minecraft:news", mojangService.GetMinecraftNewsAsync);

    public ValueTask<IEnumerable<Exhibit>> GetFeaturedModpacksAsync() =>
        GetOrCreate("repository:featured",
                    async () =>
                    {
                        var handle = await agent.SearchAsync(CurseForgeHelper.LABEL,
                                                             string.Empty,
                                                             new(null, null, ResourceKind.Modpack, null));
                        var exhibits = await handle.FetchAsync(CancellationToken.None);
                        var models = exhibits.Take(5);
                        return models;
                    });

    private ValueTask<T> GetOrCreate<T>(string key, Func<Task<T>> factory, bool cachedEnabled = true)
    {
        // WARNING: 缓存进行中的 Task 实现并发去重；已知失败/取消的 Task 不复用，让下次调用重试，
        //  否则瞬态故障（网络抖动）会把异常钉死在缓存里直到过期。
        if (cachedEnabled
            && cache.TryGetValue(key, out var cached)
            && cached is Task<T> task
            && !task.IsFaulted
            && !task.IsCanceled)
        {
            return new(task);
        }

        var rv = Task.Run(factory);
        if (cachedEnabled)
        {
            cache.Set(key, rv, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = EXPIRED_IN,
                Size = 1
            });
        }

        return new(rv);
    }
}
