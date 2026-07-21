using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using AvaloniaGraphControl;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.Avalonia.Assets;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using TridentCore.Abstractions.FileModels;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Services;
using TridentCore.Pref;

namespace Polymerium.Avalonia.ModalModels;

public partial class InstanceDependencyGraphModalModel(
    IViewContext<InstanceBasicModel> context,
    ProfileManager profileManager,
    DataService dataService,
    NotificationService notificationService
) : ViewModelBase
{
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

    [ObservableProperty]
    public partial ObservableCollection<DependencyEntry> SelectedDependencies { get; private set; }
        = [];

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

    [RelayCommand]
    private void SelectNode(string? key)
    {
        SelectedNode?.IsSelected = false;

        if (string.IsNullOrEmpty(key) || !_nodes.TryGetValue(key, out var node))
        {
            SelectedNode = null;
            SelectedDependencies.Clear();
            SelectedDependents.Clear();
            return;
        }

        SelectedNode = node;
        node.IsSelected = true;

        SelectedDependencies.Clear();
        SelectedDependents.Clear();

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
                SelectedDependencies.Add(entry);
            }
        }

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
            if (PackageHelper.TryParse(entry.Pref, out var pref))
            {
                layer.Add(new(pref.Repository, pref.Namespace, pref.Identity, pref.Version));
                installedKeys.Add(NodeKey(pref.Repository, pref.Namespace, pref.Identity));
            }
        }

        if (layer.Count == 0)
            return GraphBuildResult.Empty;

        token.ThrowIfCancellationRequested();

        // 沿 required 依赖穿透整个闭包。optional 依赖不参与解析，
        // 但其目标若被某条 required 链带入图内，仍会作为边保留。
        var resolved = new Dictionary<string, Package>();
        var requested = new Dictionary<string, PackageIdentifier>();
        var visited = new HashSet<string>(installedKeys);

        foreach (var id in layer)
            requested[NodeKey(id.Repository, id.Namespace, id.Identity)] = id;

        while (layer.Count > 0)
        {
            token.ThrowIfCancellationRequested();
            var batch = await dataService
                              .ResolvePackagesAsync(layer, Filter.None)
                              .ConfigureAwait(false);

            foreach (var (_, pkg) in batch.Successful)
                resolved[NodeKey(pkg.Label, pkg.Namespace, pkg.ProjectId)] = pkg;

            var nextLayer = new List<PackageIdentifier>();
            foreach (var (_, pkg) in batch.Successful)
            {
                var pkgKey = NodeKey(pkg.Label, pkg.Namespace, pkg.ProjectId);
                foreach (var dep in pkg.Dependencies)
                {
                    if (!dep.IsRequired)
                        continue;
                    var depKey = NodeKey(dep.Label, dep.Namespace, dep.ProjectId);
                    if (depKey == pkgKey || !visited.Add(depKey))
                        continue;
                    var depId = new PackageIdentifier(dep.Label, dep.Namespace, dep.ProjectId, dep.VersionId);
                    requested[depKey] = depId;
                    nextLayer.Add(depId);
                }
            }

            layer = nextLayer;
        }

        var thumbnailByKey = await PrefetchThumbnailsAsync(resolved, token);

        var nodes = new Dictionary<string, DependencyGraphNode>();
        foreach (var (key, pkg) in resolved)
        {
            nodes[key] = new(key,
                             pkg.Label,
                             pkg.ProjectName,
                             pkg.Namespace,
                             pkg.ProjectId,
                             pkg.Kind,
                             pkg.Author,
                             thumbnailByKey.TryGetValue(key, out var bmp) ? bmp : AssetUriIndex.DirtImageBitmap,
                             pkg.ReleaseType)
            { IsMissing = !installedKeys.Contains(key) };
        }

        // requested 记录所有发起过的解析请求，此处为解析失败的依赖兜底建节点
        foreach (var (key, id) in requested)
        {
            if (nodes.ContainsKey(key))
                continue;
            nodes[key] = new(key,
                             id.Repository,
                             id.Identity,
                             id.Namespace,
                             id.Identity,
                             ResourceKind.Mod,
                             null,
                             AssetUriIndex.DirtImageBitmap,
                             ReleaseType.Release)
            { IsMissing = true };
        }

        var graph = new Graph { Orientation = Graph.Orientations.Horizontal };
        var outgoing = new Dictionary<string, int>();
        var incoming = new Dictionary<string, List<string>>();
        var connected = new HashSet<string>();

        foreach (var (parentKey, pkg) in resolved)
        {
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

        foreach (var (key, node) in nodes)
        {
            node.DependencyCount = outgoing.GetValueOrDefault(key);
            node.DependentCount = incoming.TryGetValue(key, out var list) ? list.Count : 0;
        }

        var missing = nodes.Count(static n => n.Value.IsMissing);
        return new(graph,
                   resolved,
                   nodes,
                   incoming,
                   Total: nodes.Count,
                   Visible: connected.Count,
                   Hidden: nodes.Count - connected.Count,
                   Edges: graph.Edges.Count,
                   Missing: missing);
    }

    private async Task<Dictionary<string, Bitmap>> PrefetchThumbnailsAsync(
        Dictionary<string, Package> resolved,
        CancellationToken token
    )
    {
        token.ThrowIfCancellationRequested();
        var results = await Task.WhenAll(resolved
            .Where(kv => kv.Value.Thumbnail is not null)
            .Select(async kv =>
            {
                try
                {
                    return (kv.Key, Bmp: (Bitmap?)await dataService.GetBitmapAsync(kv.Value.Thumbnail!));
                }
                catch
                {
                    return (kv.Key, Bmp: (Bitmap?)null);
                }
            }));
        return results
            .Where(x => x.Bmp is not null)
            .ToDictionary(x => x.Key, x => x.Bmp!);
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
    )
    {
        public static GraphBuildResult Empty { get; } = new(new(),
                                                             new Dictionary<string, Package>(),
                                                             new Dictionary<string, DependencyGraphNode>(),
                                                             new Dictionary<string, List<string>>(),
                                                             0, 0, 0, 0, 0);
    }
}
