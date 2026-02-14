using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Caching.Memory;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;
using Trident.Core.Models.MojangLauncherApi;
using Trident.Core.Models.PrismLauncherApi;
using Trident.Core.Services;
using Trident.Core.Utilities;
using Version = Trident.Abstractions.Repositories.Resources.Version;

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
    MojangService mojangService)
{
    private static readonly TimeSpan EXPIRED_IN = TimeSpan.FromHours(12);

    public async ValueTask<Package> IdentifyVersionAsync(string filePath) => await agent.IdentityAsync(filePath);

    public ValueTask<Package> ResolvePackageAsync(
        string label,
        string? ns,
        string pid,
        string? vid,
        Filter filter,
        bool cachedEnabled = true) =>
        GetOrCreate($"package:{PackageHelper.Identify(label, ns, pid, vid, filter)}",
                    () => agent.ResolveAsync(label, ns, pid, vid, filter, cachedEnabled),
                    cachedEnabled);

    public async ValueTask<IReadOnlyList<Package>> ResolvePackagesAsync(
        IEnumerable<(string label, string? ns, string pid, string? vid)> batch,
        Filter filter)
    {
        var batchArray = batch.ToArray();
        var cachedTasks = batchArray
                         .Select(x => (Meta: x,
                                       Task:
                                       Get<Package>($"package:{PackageHelper.Identify(x.label, x.ns, x.pid, x.vid, filter)}")))
                         .Where(x => x.Task.HasValue)
                         .Select(async x => (x.Meta, Package: await x.Task!.Value))
                         .ToList();

        await Task.WhenAll(cachedTasks).ConfigureAwait(false);
        var cached = cachedTasks.ConvertAll(x => x.Result);
        var toResolve = batchArray.Except(cached.Select(x => x.Meta));
        var resolved = await agent.ResolveBatchAsync(toResolve, filter).ConfigureAwait(false);
        foreach (var package in resolved)
        {
            Set($"package:{PackageHelper.Identify(package.Label, package.Namespace, package.ProjectId, package.VersionId, filter)}",
                package);
        }

        return cached.Select(x => x.Package).Concat(resolved).ToList();
    }

    public ValueTask<Bitmap> GetBitmapAsync(Uri url, int widthDesired = 64) =>
        GetOrCreate($"bitmap:{url.AbsoluteUri}:{widthDesired}",
                    async () =>
                    {
                        var bytes = await agent.SeeAsync(url);
                        return Bitmap.DecodeToWidth(new MemoryStream(bytes), 64);
                    });

    public ValueTask<Project> QueryProjectAsync(string label, string? ns, string pid) =>
        GetOrCreate($"project:{PackageHelper.Identify(label, ns, pid, null, null)}",
                    () => agent.QueryAsync(label, ns, pid));

    public async ValueTask<IReadOnlyList<Project>> QueryProjectsAsync(
        IEnumerable<(string label, string? ns, string pid)> batch)
    {
        var batchArray = batch.ToArray();
        var cachedTasks = batchArray
                         .Select(x => (Meta: x,
                                       Task:
                                       Get<Project>($"project:{PackageHelper.Identify(x.label, x.ns, x.pid, null, null)}")))
                         .Where(x => x.Task.HasValue)
                         .Select(async x => (x.Meta, Project: await x.Task!.Value))
                         .ToList();
        await Task.WhenAll(cachedTasks).ConfigureAwait(false);
        var cached = cachedTasks.ConvertAll(x => x.Result);
        var toQuery = batchArray.Except(cached.Select(x => x.Meta));
        var queried = await agent.QueryBatchAsync(toQuery).ConfigureAwait(false);
        foreach (var project in queried)
        {
            Set($"project:{PackageHelper.Identify(project.Label, project.Namespace, project.ProjectId, null, null)}",
                project);
        }

        return cached.Select(x => x.Project).Concat(queried).ToList();
    }

    public ValueTask<string> ReadDescriptionAsync(string label, string? ns, string pid) =>
        GetOrCreate($"description:{PackageHelper.Identify(label, ns, pid, null, null)}",
                    () => agent.ReadDescriptionAsync(label, ns, pid));

    public ValueTask<string> ReadChangelogAsync(string label, string? ns, string pid, string vid) =>
        GetOrCreate($"changelog:{PackageHelper.Identify(label, ns, pid, vid, null)}",
                    () => agent.ReadChangelogAsync(label, ns, pid, vid));

    public ValueTask<IEnumerable<Version>> InspectVersionsAsync(string label, string? ns, string pid, Filter filter) =>
        GetOrCreate($"versions:{label}:{PackageHelper.Identify(label, ns, pid, null, filter)}",
                    async () =>
                    {
                        // DataService 一半都是前端调用
                        // 真的需要拉取全部版本的情况下只有需要版本匹配的时候都会再次级进行处理
                        // 此处进行限制避免遇到版本过多
                        const int LIMIT = 20;
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
                        } while (rv.Count != lastCount && rv.Count < LIMIT);

                        return rv.AsEnumerable();
                    });

    public ValueTask<ComponentIndex> GetComponentAsync(string loaderId) =>
        GetOrCreate($"loader:{loaderId}",
                    () => prismLauncherService.GetVersionsAsync(PrismLauncherService.UidMappings[loaderId],
                                                                CancellationToken.None));

    public ValueTask<IReadOnlyList<ComponentIndex.ComponentVersion>>
        GetComponentVersionsAsync(string loaderId, string gameVersion) =>
        GetOrCreate($"loader:{loaderId}:{gameVersion}",
                    () => prismLauncherService.GetVersionsForMinecraftVersionAsync(PrismLauncherService.UidMappings
                            [loaderId],
                        gameVersion,
                        CancellationToken.None));

    public ValueTask<ComponentIndex> GetMinecraftVersionsAsync() =>
        GetOrCreate("minecraft:versions", () => prismLauncherService.GetMinecraftVersionsAsync(CancellationToken.None));

    public ValueTask<MinecraftReleasePatchesResponse> GetMinecraftReleasePatchesAsync() =>
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


    public ValueTask<RepositoryStatus> CheckStatusAsync(string label) =>
        GetOrCreate($"repository:{label}:status", () => agent.CheckStatusAsync(label));

    private ValueTask<T>? Get<T>(string key) =>
        cache.TryGetValue(key, out var cached) && cached is ValueTask<T> res ? res : null;

    private void Set<T>(string key, T value) => cache.Set(key, new ValueTask<T>(value), EXPIRED_IN);

    private ValueTask<T> GetOrCreate<T>(string key, Func<Task<T>> factory, bool cachedEnabled = true)
    {
        if (cachedEnabled
         && cache.TryGetValue(key, out var cached)
         && cached is ValueTask<T> { IsCompletedSuccessfully: true } task)
        {
            return task;
        }

        var rv = new ValueTask<T>(Task.Run(factory));
        if (cachedEnabled)
        {
            cache.Set(key, rv, EXPIRED_IN);
        }

        return rv;
    }
}
