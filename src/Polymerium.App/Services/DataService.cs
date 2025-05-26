using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    MojangLauncherService mojangLauncherService)
{
    private static readonly TimeSpan EXPIRED_IN = TimeSpan.FromHours(12);

    public ValueTask<Package> ResolvePackageAsync(
        string label,
        string? ns,
        string pid,
        string? vid,
        Filter filter,
        bool cachedEnabled = true) =>
        GetOrCreate($"package:{PackageHelper.Identify(label, ns, pid, vid, filter)}",
                    () => agent.ResolveAsync(label, ns, pid, vid, filter, cachedEnabled));

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
                        } while (rv.Count != lastCount);

                        return rv.AsEnumerable();
                    });

    public ValueTask<ComponentIndex> GetComponentAsync(string loaderId) =>
        GetOrCreate($"loader:{loaderId}",
                    () => prismLauncherService.GetVersionsAsync(PrismLauncherService.UID_MAPPINGS[loaderId],
                                                                CancellationToken.None));

    public ValueTask<IReadOnlyList<ComponentIndex.ComponentVersion>>
        GetComponentVersionsAsync(string loaderId, string gameVersion) =>
        GetOrCreate($"loader:{loaderId}:{gameVersion}",
                    () => prismLauncherService.GetVersionsForMinecraftVersionAsync(PrismLauncherService.UID_MAPPINGS
                            [loaderId],
                        gameVersion,
                        CancellationToken.None));

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
                              return new ValueTask<T>(Task.Run(async () =>
                              {
                                  try
                                  {
                                      return await factory();
                                  }
                                  catch
                                  {
                                      entry.SetSlidingExpiration(TimeSpan.FromSeconds(1));
                                      throw;
                                  }
                              }));
                          });
}