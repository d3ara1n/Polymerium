using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaGraphControl;
using CommunityToolkit.Mvvm.ComponentModel;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using TridentCore.Abstractions.FileModels;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Services;
using TridentCore.Purl;

namespace Polymerium.Avalonia.ModalModels;

public partial class InstanceDependencyGraphModalModel(
    IViewContext<InstanceBasicModel> context,
    ProfileManager profileManager,
    DataService dataService,
    NotificationService notificationService
) : ViewModelBase
{
    #region Direct

    public InstanceBasicModel Basic { get; } = context.Parameter!;

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial Graph? DependencyGraph { get; private set; }

    #endregion

    #region Overrides

    protected override async Task OnInitializeAsync(CancellationToken token)
    {
        if (!profileManager.TryGetImmutable(Basic.Key, out var profile))
        {
            notificationService.PopMessage("Instance not found", "Dependency Graph");
            return;
        }

        try
        {
            // 构建依赖图是 I/O + CPU 混合任务，丢到后台线程避免阻塞 UI。
            DependencyGraph = await Task.Run(() => BuildGraphAsync(profile, token), token);
        }
        catch (System.Exception ex)
        {
            notificationService.PopMessage(ex, "Failed to build dependency graph");
        }
    }

    #endregion

    /// <summary>
    ///     节点 key：(Label, Namespace, ProjectId) 三元组拼接，避免不同仓库同名项目冲突。
    /// </summary>
    private static string NodeKey(string label, string? ns, string pid) => $"{label}|{ns ?? string.Empty}|{pid}";

    private async Task<Graph> BuildGraphAsync(Profile profile, CancellationToken token)
    {
        // 1. 解析已安装包的 purl → PackageIdentifier
        var installed = new List<PackageIdentifier>();
        foreach (var entry in profile.Setup.Packages)
        {
            if (PackageHelper.TryParse(entry.Purl, out var purl))
            {
                installed.Add(new PackageIdentifier(purl.Label, purl.Namespace, purl.Pid, purl.Vid));
            }
        }

        if (installed.Count == 0)
        {
            return new Graph();
        }

        token.ThrowIfCancellationRequested();

        // 2. 批量解析已安装包 → Package（含 Dependencies 与 ProjectName）
        var packages = await dataService
                            .ResolvePackagesAsync(installed, Filter.None)
                            .ConfigureAwait(false);

        token.ThrowIfCancellationRequested();

        // 3. 建立已安装包节点字典：key = (Label, Namespace, ProjectId) → node
        //    只画已安装包之间的依赖关系，网络上的依赖项不拉取、不绘制。
        var nodes = new Dictionary<string, DependencyGraphNode>();
        foreach (var (_, pkg) in packages)
        {
            var key = NodeKey(pkg.Label, pkg.Namespace, pkg.ProjectId);
            nodes[key] = new DependencyGraphNode(key, pkg.ProjectName);
        }

        // 4. 只连「指向另一个已安装包」的依赖边。
        //    GraphPanel 内部从 Edges 推导节点集合，因此只加边即可——
        //    没有任何边（既不依赖也不被依赖）的孤儿包自然不会出现在图里。
        var graph = new Graph { Orientation = Graph.Orientations.Horizontal };
        foreach (var (_, pkg) in packages)
        {
            var parentKey = NodeKey(pkg.Label, pkg.Namespace, pkg.ProjectId);
            if (!nodes.TryGetValue(parentKey, out var parent))
            {
                continue;
            }

            foreach (var dep in pkg.Dependencies)
            {
                var depKey = NodeKey(dep.Label, dep.Namespace, dep.ProjectId);
                // 跳过自环与指向未安装包的依赖
                if (depKey == parentKey)
                {
                    continue;
                }

                if (nodes.TryGetValue(depKey, out var child))
                {
                    graph.Edges.Add(new Edge(parent, child));
                }
            }
        }

        return graph;
    }
}
