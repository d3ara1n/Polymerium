using System;
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

    private static string NodeKey(string label, string? ns, string pid) => $"{label}|{ns ?? string.Empty}|{pid}";

    private async Task<GraphBuildResult> BuildGraphAsync(Profile profile, CancellationToken token)
    {
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
            return new GraphBuildResult(new Graph(), 0, 0, 0, 0);
        }

        token.ThrowIfCancellationRequested();

        var packages = await dataService
                            .ResolvePackagesAsync(installed, Filter.None)
                            .ConfigureAwait(false);

        token.ThrowIfCancellationRequested();

        var nodes = new Dictionary<string, DependencyGraphNode>();
        foreach (var (_, pkg) in packages)
        {
            var key = NodeKey(pkg.Label, pkg.Namespace, pkg.ProjectId);
            nodes[key] = new DependencyGraphNode(key, pkg.ProjectName);
        }

        var graph = new Graph { Orientation = Graph.Orientations.Horizontal };
        var connected = new HashSet<string>();
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
                if (depKey == parentKey)
                {
                    continue;
                }

                if (nodes.TryGetValue(depKey, out var child))
                {
                    graph.Edges.Add(new Edge(parent, child));
                    connected.Add(parentKey);
                    connected.Add(depKey);
                }
            }
        }

        return new GraphBuildResult(
            graph,
            Total: nodes.Count,
            Visible: connected.Count,
            Hidden: nodes.Count - connected.Count,
            Edges: graph.Edges.Count
        );
    }

    private record GraphBuildResult(Graph Graph, int Total, int Visible, int Hidden, int Edges);
}
