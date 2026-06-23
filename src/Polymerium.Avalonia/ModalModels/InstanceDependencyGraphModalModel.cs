using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    // 构图后保留，供详情面板按选中节点计算前置/附属
    private IReadOnlyDictionary<string, Package> _packages = new Dictionary<string, Package>();
    private IReadOnlyDictionary<string, DependencyGraphNode> _nodes = new Dictionary<string, DependencyGraphNode>();
    private IReadOnlyDictionary<string, List<string>> _incoming = new Dictionary<string, List<string>>();

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
    public partial int MissingCount { get; private set; }

    [ObservableProperty]
    public partial DependencyGraphNode? SelectedNode { get; private set; }

    // 前置：当前包依赖的图内节点（出边）
    [ObservableProperty]
    public partial ObservableCollection<DependencyEntry> SelectedPrerequisites { get; private set; }
        = [];

    // 附属：依赖当前包的图内节点（入边）
    [ObservableProperty]
    public partial ObservableCollection<DependencyEntry> SelectedDependents { get; private set; }
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
            _incoming = result.Incoming;
            TotalPackages = result.Total;
            VisiblePackages = result.Visible;
            HiddenPackages = result.Hidden;
            EdgeCount = result.Edges;
            MissingCount = result.Missing;
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

    /// <summary>选中某个节点（按 Key），刷新右侧详情面板的前置与附属列表。传 null 清空选中。</summary>
    [RelayCommand]
    private void SelectNode(string? key)
    {
        SelectedNode?.IsSelected = false;

        if (string.IsNullOrEmpty(key) || !_nodes.TryGetValue(key, out var node))
        {
            SelectedNode = null;
            SelectedPrerequisites.Clear();
            SelectedDependents.Clear();
            return;
        }

        SelectedNode = node;
        node.IsSelected = true;

        SelectedPrerequisites.Clear();
        SelectedDependents.Clear();

        // 前置（出边）：当前包依赖的图内节点
        if (_packages.TryGetValue(node.Key, out var pkg))
        {
            foreach (var entry in pkg.Dependencies
                                     .Select(dep =>
                                      {
                                          var depKey = NodeKey(dep.Label, dep.Namespace, dep.ProjectId);
                                          return _nodes.TryGetValue(depKey, out var depNode)
                                              ? new DependencyEntry(depKey,
                                                                    depNode.ProjectName,
                                                                    dep.IsRequired,
                                                                    depNode.IsMissing)
                                              : null;
                                      })
                                     .Where(e => e is not null)
                                     .Cast<DependencyEntry>()
                                     .OrderByDescending(e => e.IsRequired)
                                     .ThenBy(e => e.ProjectName, StringComparer.CurrentCultureIgnoreCase))
            {
                SelectedPrerequisites.Add(entry);
            }
        }

        // 附属（入边）：依赖当前包的图内节点
        if (_incoming.TryGetValue(node.Key, out var dependents))
        {
            foreach (var entry in dependents
                                   .Select(depKey =>
                                    {
                                        if (!_nodes.TryGetValue(depKey, out var depNode))
                                            return null;
                                        var isRequired = _packages.TryGetValue(depKey, out var depPkg)
                                         && depPkg.Dependencies.Any(d =>
                                            NodeKey(d.Label, d.Namespace, d.ProjectId) == node.Key && d.IsRequired);
                                        return new DependencyEntry(depKey,
                                                                   depNode.ProjectName,
                                                                   isRequired,
                                                                   depNode.IsMissing);
                                    })
                                   .Where(e => e is not null)
                                   .Cast<DependencyEntry>()
                                   .OrderByDescending(e => e.IsRequired)
                                   .ThenBy(e => e.ProjectName, StringComparer.CurrentCultureIgnoreCase))
            {
                SelectedDependents.Add(entry);
            }
        }
    }

    #endregion

    private static string NodeKey(string label, string? ns, string pid) => $"{label}|{ns ?? string.Empty}|{pid}";

    private async Task<GraphBuildResult> BuildGraphAsync(Profile profile, CancellationToken token)
    {
        var installedKeys = new HashSet<string>();
        var layer = new List<PackageIdentifier>();
        foreach (var entry in profile.Setup.Packages)
        {
            if (PackageHelper.TryParse(entry.Purl, out var purl))
            {
                layer.Add(new(purl.Label, purl.Namespace, purl.Pid, purl.Vid));
                installedKeys.Add(NodeKey(purl.Label, purl.Namespace, purl.Pid));
            }
        }

        if (layer.Count == 0)
        {
            return new(new(),
                       new Dictionary<string, Package>(),
                       new Dictionary<string, DependencyGraphNode>(),
                       new Dictionary<string, List<string>>(),
                       0, 0, 0, 0, 0);
        }

        token.ThrowIfCancellationRequested();

        // 广度优先逐层批量解析：已安装包为第一层，每层解析完后从「已安装」包的依赖构建下一层。
        // 缺失依赖（未安装）也会被解析以获取展示元数据，但作为叶子不再向下展开。
        var resolved = new Dictionary<string, Package>();
        var visited = new HashSet<string>(installedKeys);

        while (layer.Count > 0)
        {
            token.ThrowIfCancellationRequested();
            var batch = await dataService
                              .ResolvePackagesAsync(layer, Filter.None)
                              .ConfigureAwait(false);

            var nextLayer = new List<PackageIdentifier>();

            foreach (var (_, pkg) in batch)
            {
                var key = NodeKey(pkg.Label, pkg.Namespace, pkg.ProjectId);
                resolved[key] = pkg;
            }

            // 只从「已安装」包向下追依赖；缺失包作为叶子
            foreach (var (_, pkg) in batch)
            {
                var pkgKey = NodeKey(pkg.Label, pkg.Namespace, pkg.ProjectId);
                if (!installedKeys.Contains(pkgKey))
                    continue;

                foreach (var dep in pkg.Dependencies)
                {
                    var depKey = NodeKey(dep.Label, dep.Namespace, dep.ProjectId);
                    if (depKey == pkgKey)
                        continue;

                    if (visited.Add(depKey))
                        nextLayer.Add(new(dep.Label, dep.Namespace, dep.ProjectId, dep.VersionId));
                }
            }

            layer = nextLayer;
        }

        // 识别 required 缺失：已安装包声明的 required 依赖，其 key 不在已安装集合
        var missingDeps = new Dictionary<string, Dependency>();
        foreach (var (key, pkg) in resolved)
        {
            if (!installedKeys.Contains(key))
                continue;

            foreach (var dep in pkg.Dependencies)
            {
                var depKey = NodeKey(dep.Label, dep.Namespace, dep.ProjectId);
                if (dep.IsRequired && !installedKeys.Contains(depKey))
                    missingDeps[depKey] = dep;
            }
        }

        // 构建节点
        var nodes = new Dictionary<string, DependencyGraphNode>();
        foreach (var (key, pkg) in resolved)
        {
            if (installedKeys.Contains(key))
            {
                nodes[key] = new(key,
                                 pkg.Label,
                                 pkg.ProjectName,
                                 pkg.Namespace,
                                 pkg.ProjectId,
                                 pkg.Kind,
                                 pkg.Author,
                                 pkg.Thumbnail,
                                 pkg.ReleaseType);
            }
        }

        foreach (var (key, dep) in missingDeps)
        {
            if (nodes.ContainsKey(key))
                continue;

            if (resolved.TryGetValue(key, out var pkg))
            {
                nodes[key] = new(key,
                                 pkg.Label,
                                 pkg.ProjectName,
                                 pkg.Namespace,
                                 pkg.ProjectId,
                                 pkg.Kind,
                                 pkg.Author,
                                 pkg.Thumbnail,
                                 pkg.ReleaseType) { IsMissing = true };
            }
            else
            {
                // 完全无法解析，用依赖声明兜底
                nodes[key] = new(key,
                                 dep.Label,
                                 dep.ProjectId,
                                 dep.Namespace,
                                 dep.ProjectId,
                                 ResourceKind.Mod,
                                 null,
                                 null,
                                 ReleaseType.Release) { IsMissing = true };
            }
        }

        // 构建边（只从已安装包出边，连向图内节点）+ 计数 + 入边索引
        var graph = new Graph { Orientation = Graph.Orientations.Horizontal };
        var outgoing = new Dictionary<string, int>();
        var incoming = new Dictionary<string, List<string>>();
        var connected = new HashSet<string>();

        foreach (var (parentKey, pkg) in resolved)
        {
            if (!installedKeys.Contains(parentKey))
                continue;
            if (!nodes.TryGetValue(parentKey, out var parent))
                continue;

            foreach (var dep in pkg.Dependencies)
            {
                var depKey = NodeKey(dep.Label, dep.Namespace, dep.ProjectId);
                if (depKey == parentKey)
                    continue;
                if (!nodes.TryGetValue(depKey, out var child))
                    continue;

                graph.Edges.Add(new(parent, child));
                outgoing[parentKey] = outgoing.GetValueOrDefault(parentKey) + 1;
                if (!incoming.TryGetValue(depKey, out var list))
                    incoming[depKey] = list = new();
                if (!list.Contains(parentKey))
                    list.Add(parentKey);
                connected.Add(parentKey);
                connected.Add(depKey);
            }
        }

        // 回填计数
        foreach (var (key, node) in nodes)
        {
            node.PrerequisiteCount = outgoing.GetValueOrDefault(key);
            node.DependentCount = incoming.TryGetValue(key, out var list) ? list.Count : 0;
        }

        return new(graph,
                   resolved,
                   nodes,
                   incoming,
                   Total: nodes.Count,
                   Visible: connected.Count,
                   Hidden: nodes.Count - connected.Count,
                   Edges: graph.Edges.Count,
                   Missing: missingDeps.Count);
    }

    private record GraphBuildResult(
        Graph Graph,
        IReadOnlyDictionary<string, Package> Packages,
        IReadOnlyDictionary<string, DependencyGraphNode> Nodes,
        IReadOnlyDictionary<string, List<string>> Incoming,
        int Total,
        int Visible,
        int Hidden,
        int Edges,
        int Missing
    );
}
