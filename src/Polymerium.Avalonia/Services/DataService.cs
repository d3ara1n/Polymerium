using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
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

// NOTE: Application 级数据整合服务，所有 API/模型统一经此提供；状态全局共享，故无需取消。
public class DataService(
    IMemoryCache cache,
    RepositoryAgent agent,
    PrismLauncherService prismLauncherService,
    MojangService mojangService,
    IHttpClientFactory httpClientFactory)
{
    private static readonly TimeSpan EXPIRED_IN = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ICON_FILE_EXPIRED_IN = TimeSpan.FromDays(30);

    public async ValueTask<Package> IdentifyVersionAsync(string filePath) => await agent.IdentifyAsync(filePath);

    // NOTE: Package/Project/Description/Changelog/Status 缓存归 Trident 仓库缓存层管，此处直接委托；
    //  DataService 只缓存 UI hot data 与应用层加工后的数据。
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

    // NOTE: 以下为 DataService 独有的内存缓存——数据源不在 RepositoryAgent，或经过额外加工（Bitmap 解码、版本数截断）。

    public ValueTask<Bitmap> GetBitmapAsync(Uri url, int maxWidth = 64)
    {
        var key = $"bitmap:{maxWidth}:{url.AbsoluteUri}";

        // NOTE: 第一层内存缓存——进行中的 Task 也在缓存里，天然去重。
        if (cache.TryGetValue(key, out var cached) && cached is Task<Bitmap> task)
        {
            return new(task);
        }

        var rv = LoadOrDownloadBitmapAsync(url, maxWidth);
        var entry = cache.CreateEntry(key);
        entry.AbsoluteExpirationRelativeToNow = EXPIRED_IN;
        entry.Size = 1;
        entry.Value = rv;
        // NOTE: 驱逐时不释放 Bitmap——UI 可能仍持有引用，提前释放抛 ObjectDisposedException；
        //  非托管资源由 GC finalizer 在引用全部消失后回收。
        entry.Dispose();
        return new(rv);
    }

    private async Task<Bitmap> LoadOrDownloadBitmapAsync(Uri url, int maxWidth)
    {
        var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(url.AbsoluteUri))).ToLowerInvariant();
        var path = PathDef.Default.FileOfIconObject(hash);

        byte[] bytes;
        if (File.Exists(path) && File.GetLastWriteTimeUtc(path) + ICON_FILE_EXPIRED_IN > DateTime.UtcNow)
        {
            bytes = await File.ReadAllBytesAsync(path);
        }
        else
        {
            using var client = httpClientFactory.CreateClient();
            bytes = await client.GetByteArrayAsync(url);

            // NOTE: 先写临时文件再 rename，崩溃时不会留下损坏文件。
            var dir = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(dir);
            var tmp = path + ".tmp";
            await File.WriteAllBytesAsync(tmp, bytes);
            File.Move(tmp, path, true);
        }

        using var memory = new MemoryStream(bytes);
        return Bitmap.DecodeToWidth(memory, maxWidth, BitmapInterpolationMode.LowQuality);
    }

    public ValueTask<IEnumerable<Version>> InspectVersionsAsync(string label, string? ns, string pid, Filter filter) =>
        GetOrCreate($"versions:{label}:{PackageHelper.Identify(label, ns, pid, null, filter)}",
                    async () =>
                    {
                        // NOTE: 调用以读展示数据为主，仅版本匹配需全量；此处设上限避免一次拉取过多。
                        const int LIMIT = 20;
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
                        } while (rv.Count != lastCount && rv.Count < LIMIT);

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
                                                             new(null, null, ResourceKind.Modpack));
                        var exhibits = await handle.FetchAsync(CancellationToken.None);
                        var models = exhibits.Take(5);
                        return models;
                    });

    private ValueTask<T> GetOrCreate<T>(string key, Func<Task<T>> factory, bool cachedEnabled = true)
    {
        if (cachedEnabled && cache.TryGetValue(key, out var cached) && cached is Task<T> task)
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
