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
using TridentCore.Purl;
using Version = TridentCore.Abstractions.Repositories.Resources.Version;

namespace Polymerium.App.Services;

// Application 级别的数据整合服务，所有 API 和模型均为 UI 服务
// 后续其他 VM 中用到的数据提供方都会切换为 nameof(DataService)

// 提供全局的数据管理
// 例如 Package Resolving 时可以从这里拿到 ValueTask
// 由于状态是共享的，所以根本不需要取消
public class DataService(
    IMemoryCache cache,
    RepositoryAgent agent,
    PrismLauncherService prismLauncherService,
    MojangService mojangService,
    IHttpClientFactory httpClientFactory
)
{
    private static readonly TimeSpan EXPIRED_IN = TimeSpan.FromHours(12);
    private static readonly TimeSpan ICON_FILE_EXPIRED_IN = TimeSpan.FromDays(30);

    public async ValueTask<Package> IdentifyVersionAsync(string filePath) =>
        await agent.IdentityAsync(filePath);

    // Package / Project / Description / Changelog / Status 的缓存统一由
    // RepositoryAgent 的分布式缓存(SQLite)管理，此处直接委托。
    public Task<Package> ResolvePackageAsync(
        string label,
        string? ns,
        string pid,
        string? vid,
        Filter filter,
        bool cachedEnabled = true
    ) => agent.ResolveAsync(label, ns, pid, vid, filter, cachedEnabled);

    public Task<IReadOnlyList<(PackageIdentifier, Package)>> ResolvePackagesAsync(
        IEnumerable<PackageIdentifier> batch,
        Filter filter
    ) => agent.ResolveBatchAsync(batch, filter);

    public Task<Project> QueryProjectAsync(string label, string? ns, string pid) =>
        agent.QueryAsync(label, ns, pid);

    public Task<IReadOnlyList<Project>> QueryProjectsAsync(
        IEnumerable<(string label, string? ns, string pid)> batch
    ) => agent.QueryBatchAsync(batch);

    public Task<string> ReadDescriptionAsync(string label, string? ns, string pid) =>
        agent.ReadDescriptionAsync(label, ns, pid);

    public Task<string> ReadChangelogAsync(string label, string? ns, string pid, string vid) =>
        agent.ReadChangelogAsync(label, ns, pid, vid);

    public Task<RepositoryStatus> CheckStatusAsync(string label) =>
        agent.CheckStatusAsync(label);

    // 以下为 DataService 独有的内存缓存，数据源不在 RepositoryAgent 中
    // 或经过额外处理（如 Bitmap 解码、版本数量截断）

    public ValueTask<Bitmap> GetBitmapAsync(Uri url)
    {
        var key = $"bitmap:{url.AbsoluteUri}";

        // 第一层：内存缓存（包括进行中的 Task，天然去重）
        if (cache.TryGetValue(key, out var cached) && cached is Task<Bitmap> task)
            return new(task);

        var rv = LoadOrDownloadBitmapAsync(url);
        var entry = cache.CreateEntry(key);
        entry.AbsoluteExpirationRelativeToNow = EXPIRED_IN;
        entry.Value = rv;
        entry.RegisterPostEvictionCallback((_, value, _, _) =>
        {
            if (value is Task<Bitmap> { Result: Bitmap bmp })
                bmp.Dispose();
        });
        entry.Dispose();
        return new(rv);
    }

    private async Task<Bitmap> LoadOrDownloadBitmapAsync(Uri url)
    {
        var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(url.AbsoluteUri))).ToLowerInvariant();
        var path = PathDef.Default.FileOfIconObject(hash);

        // 第二层：文件缓存（30天有效期）
        byte[] bytes;
        if (File.Exists(path) && File.GetLastWriteTimeUtc(path) + ICON_FILE_EXPIRED_IN > DateTime.UtcNow)
        {
            bytes = await File.ReadAllBytesAsync(path);
        }
        else
        {
            // 第三层：下载
            using var client = httpClientFactory.CreateClient();
            bytes = await client.GetByteArrayAsync(url);

            // 写入临时文件再 rename，防止崩溃留下损坏文件
            var dir = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(dir);
            var tmp = path + ".tmp";
            await File.WriteAllBytesAsync(tmp, bytes);
            File.Move(tmp, path, overwrite: true);
        }

        return new(new MemoryStream(bytes));
    }

    public ValueTask<IEnumerable<Version>> InspectVersionsAsync(
        string label,
        string? ns,
        string pid,
        Filter filter
    ) =>
        GetOrCreate(
            $"versions:{label}:{PackageHelper.Identify(label, ns, pid, null, filter)}",
            async () =>
            {
                // DataService 一半都是前端调用
                // 真的需要拉取全部版本的情况下只有需要版本匹配的时候都会再次级进行处理
                // 此处进行限制避免遇到版本过多
                const int limit = 20;
                var handle = await agent.InspectAsync(label, ns, pid, filter);
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
            }
        );

    public ValueTask<ComponentIndex> GetComponentAsync(string loaderId) =>
        GetOrCreate(
            $"loader:{loaderId}",
            () =>
                prismLauncherService.GetVersionsAsync(
                    PrismLauncherService.UidMappings[loaderId],
                    CancellationToken.None
                )
        );

    public ValueTask<IReadOnlyList<ComponentIndex.ComponentVersion>> GetComponentVersionsAsync(
        string loaderId,
        string gameVersion
    ) =>
        GetOrCreate(
            $"loader:{loaderId}:{gameVersion}",
            () =>
                prismLauncherService.GetVersionsForMinecraftVersionAsync(
                    PrismLauncherService.UidMappings[loaderId],
                    gameVersion,
                    CancellationToken.None
                )
        );

    public ValueTask<ComponentIndex> GetMinecraftVersionsAsync() =>
        GetOrCreate(
            "minecraft:versions",
            () => prismLauncherService.GetMinecraftVersionsAsync(CancellationToken.None)
        );

    public ValueTask<MinecraftReleasePatchesResponse> GetMinecraftReleasePatchesAsync() =>
        GetOrCreate("minecraft:news", mojangService.GetMinecraftNewsAsync);

    public ValueTask<IEnumerable<Exhibit>> GetFeaturedModpacksAsync() =>
        GetOrCreate(
            "repository:featured",
            async () =>
            {
                var handle = await agent.SearchAsync(
                    CurseForgeHelper.LABEL,
                    string.Empty,
                    new(null, null, ResourceKind.MODPACK)
                );
                var exhibits = await handle.FetchAsync(CancellationToken.None);
                var models = exhibits.Take(5);
                return models;
            }
        );

    private ValueTask<T> GetOrCreate<T>(
        string key,
        Func<Task<T>> factory,
        bool cachedEnabled = true
    )
    {
        if (
            cachedEnabled
            && cache.TryGetValue(key, out var cached)
            && cached is Task<T> task
        )
        {
            return new(task);
        }

        var rv = Task.Run(factory);
        if (cachedEnabled)
        {
            cache.Set(key, rv, EXPIRED_IN);
        }

        return new(rv);
    }
}
