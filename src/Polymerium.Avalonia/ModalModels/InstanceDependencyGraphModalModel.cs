using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaGraphControl;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using TridentCore.Abstractions.FileModels;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
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
    // 构图后保留，供详情面板按选中节点计算依赖列表
    private IReadOnlyDictionary<string, Package> _packages = new Dictionary<string, Package>();
    private IReadOnlyDictionary<string, DependencyGraphNode> _nodes = new Dictionary<string, DependencyGraphNode>();

    #region Direct

    public InstanceBasicModel Basic { get; } = context.GetRequiredParameter();

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial Graph? DependencyGraph { get; private set; }

    [ObservableProperty]
    public partial bool IsLoading { get; private set; } = true;

    [ObservableProperty]
    public partial int TotalPackages { get; private set; }

    [ObservableProperty]
    public partial int VisiblePackages { get; private set; }

    [ObservableProperty]
    public partial int HiddenPackages { get; private set; }

    [ObservableProperty]
    public partial int EdgeCount { get; private set; }

    [ObservableProperty]
    public partial DependencyGraphNode? SelectedNode { get; private set; }

    [ObservableProperty]
    public partial IReadOnlyList<DependencyEntry> SelectedDependencies { get; private set; }
        = [];

    #endregion

    #region Overrides

    protected override async Task OnInitializeAsync(CancellationToken token)
    {
        if (!profileManager.TryGetImmutable(Basic.Key, out var profile))
        {
            notificationService.PopMessage("Instance not found", "Dependency Graph");
            IsLoading = false;
            return;
        }

        try
        {
            var result = await Task.Run(() => BuildGraphAsync(profile, token), token);
            DependencyGraph = result.Graph;
            _packages = result.Packages;
            _nodes = result.Nodes;
            TotalPackages = result.Total;
            VisiblePackages = result.Visible;
            HiddenPackages = result.Hidden;
            EdgeCount = result.Edges;
        }
        catch (Exception ex)
        {
            notificationService.PopMessage(ex, "Failed to build dependency graph");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Commands

    /// <summary>选中某个节点（按 Key），刷新右侧详情面板的依赖列表。传 null 清空选中。</summary>
    [RelayCommand]
    private void SelectNode(string? key)
    {
        // 先清除上一次选中的节点状态（驱动控件清除高亮）
        SelectedNode?.IsSelected = false;

        if (string.IsNullOrEmpty(key) || !_nodes.TryGetValue(key, out var node))
        {
            SelectedNode = null;
            SelectedDependencies = [];
            return;
        }

        SelectedNode = node;
        node.IsSelected = true;

        // 该节点的 Package 元数据，取所有出边依赖（依赖图只关心已安装包之间的关系，未安装依赖不显示）
        if (_packages.TryGetValue(node.Key, out var pkg))
        {
            SelectedDependencies = pkg.Dependencies
                                      .Where(dep => _packages.ContainsKey(
                                          NodeKey(dep.Label, dep.Namespace, dep.ProjectId)))
                                      .Select(dep =>
                                       {
                                           var depKey = NodeKey(dep.Label, dep.Namespace, dep.ProjectId);
                                           var depPkg = _packages[depKey];
                                           return new DependencyEntry(
                                               depKey,
                                               depPkg.ProjectName,
                                               dep.IsRequired
                                           );
                                       })
                                      .OrderByDescending(e => e.IsRequired)
                                      .ThenBy(e => e.ProjectName, StringComparer.CurrentCultureIgnoreCase)
                                      .ToList();
        }
        else
        {
            SelectedDependencies = [];
        }
    }

    #endregion

    private static string NodeKey(string label, string? ns, string pid) => $"{label}|{ns ?? string.Empty}|{pid}";

    private async Task<GraphBuildResult> BuildGraphAsync(Profile profile, CancellationToken token)
    {
        var installed = new List<PackageIdentifier>();
        foreach (var entry in profile.Setup.Packages)
        {
            if (PackageHelper.TryParse(entry.Purl, out var purl))
            {
                installed.Add(new(purl.Label, purl.Namespace, purl.Pid, purl.Vid));
            }
        }

        if (installed.Count == 0)
        {
            return new(new(), new Dictionary<string, Package>(),
                       new Dictionary<string, DependencyGraphNode>(), 0, 0, 0, 0);
        }

        token.ThrowIfCancellationRequested();

        var packages = await dataService
                            .ResolvePackagesAsync(installed, Filter.None)
                            .ConfigureAwait(false);

        token.ThrowIfCancellationRequested();

        var packageMap = new Dictionary<string, Package>();
        foreach (var (_, pkg) in packages)
        {
            packageMap[NodeKey(pkg.Label, pkg.Namespace, pkg.ProjectId)] = pkg;
        }

        var nodes = new Dictionary<string, DependencyGraphNode>();
        foreach (var pkg in packageMap.Values)
        {
            var key = NodeKey(pkg.Label, pkg.Namespace, pkg.ProjectId);
            nodes[key] = new(
                             key,
                             pkg.Label,
                             pkg.ProjectName,
                             pkg.Namespace,
                             pkg.ProjectId,
                             pkg.Kind,
                             pkg.Author,
                             pkg.Thumbnail,
                             pkg.ReleaseType
                            );
        }

        var graph = new Graph { Orientation = Graph.Orientations.Horizontal };
        var connected = new HashSet<string>();
        foreach (var pkg in packageMap.Values)
        {
            var parentKey = NodeKey(pkg.Label, pkg.Namespace, pkg.ProjectId);
            if (!nodes.TryGetValue(parentKey, out var parent))
            {
                continue;
            }

            foreach (var dep in pkg.Dependencies)
            {
                var depKey = NodeKey(dep.Label, dep.Namespace, dep.ProjectId);
                if (depKey == parentKey)
                {
                    continue;
                }

                if (nodes.TryGetValue(depKey, out var child))
                {
                    graph.Edges.Add(new(parent, child));
                    connected.Add(parentKey);
                    connected.Add(depKey);
                }
            }
        }

        return new(
                   graph,
                   packageMap,
                   nodes,
                   Total: nodes.Count,
                   Visible: connected.Count,
                   Hidden: nodes.Count - connected.Count,
                   Edges: graph.Edges.Count
                  );
    }

    private record GraphBuildResult(
        Graph Graph,
        IReadOnlyDictionary<string, Package> Packages,
        IReadOnlyDictionary<string, DependencyGraphNode> Nodes,
        int Total,
        int Visible,
        int Hidden,
        int Edges
    );
}
