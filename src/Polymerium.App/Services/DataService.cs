﻿using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Caching.Memory;
using Polymerium.Trident.Models.MojangLauncherApi;
using Polymerium.Trident.Models.PrismLauncherApi;
using Polymerium.Trident.Services;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.Services;

// Application 级别的数据整合服务，所有 API 和模型均为 UI 服务
// 后续其他 VM 中用到的数据提供方都会切换为 nameof(DataService)

// 提供全局的数据管理
// 例如 Package Resolving 时可以从这里拿到 ValueTask
// 由于状态是共享的，所以根本不需要取消
public class DataService(
    IMemoryCache cache,
    IHttpClientFactory clientFactory,
    RepositoryAgent agent,
    PrismLauncherService prismLauncherService,
    MojangLauncherService mojangLauncherService)
{
    private static readonly TimeSpan EXPIRED_IN = TimeSpan.FromHours(12);

    public ValueTask<Package> ResolvePackageAsync(string label, string? ns, string pid, string? vid, Filter filter) =>
        GetOrCreate($"package:{PackageHelper.Identify(label, ns, pid, vid, filter)}",
                    () => agent.ResolveAsync(label, ns, pid, vid, filter));

    public ValueTask<Bitmap> GetBitmapAsync(Uri url, int widthDesired = 64) =>
        GetOrCreate($"bitmap:{url.AbsoluteUri}:{widthDesired}",
                    async () =>
                    {
                        using var client = clientFactory.CreateClient();
                        var bytes = await client.GetByteArrayAsync(url);
                        return Bitmap.DecodeToWidth(new MemoryStream(bytes), 64);
                    });

    public ValueTask<ComponentIndex> GetMinecraftVersionsAsync() =>
        GetOrCreate("minecraft:versions", () => prismLauncherService.GetMinecraftVersionsAsync(CancellationToken.None));

    public ValueTask<MinecraftNewsResponse> GetMinecraftNewsAsync() =>
        GetOrCreate("minecraft:news", mojangLauncherService.GetMinecraftNewsAsync);


    public ValueTask<RepositoryStatus> CheckStatusAsync(string label) =>
        GetOrCreate($"repository:{label}:status", () => agent.CheckStatusAsync(label));


    private ValueTask<T> GetOrCreate<T>(string key, Func<Task<T>> factory) =>
        cache.GetOrCreate(key,
                          entry =>
                          {
                              entry.SetSlidingExpiration(EXPIRED_IN);
                              return new ValueTask<T>(factory());
                          });
}