using AvaloniaGraphControl;
using Huskui.Avalonia.Controls;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Modals;

public partial class InstanceDependencyGraphModal : Modal
{
    public InstanceDependencyGraphModal() => InitializeComponent();

    /// <summary>
    ///     POC 阶段：硬编码 DAG，验证 AvaloniaGraphControl 在 Avalonia 12 + .NET 10 下能否渲染。
    ///     阶段 3 接入业务数据时，改为由调用方（InstanceSetupPageModel）构建后传入。
    /// </summary>
    public Graph DependencyGraph { get; } = BuildSampleGraph();

    private static Graph BuildSampleGraph()
    {
        var minecraft = new DependencyGraphNode("Minecraft 1.20.1");
        var fabricLoader = new DependencyGraphNode("Fabric Loader 0.16.0");
        var fabricApi = new DependencyGraphNode("Fabric API");
        var sodium = new DependencyGraphNode("Sodium");
        var lithium = new DependencyGraphNode("Lithium");
        var phosphor = new DependencyGraphNode("Phosphor");

        var graph = new Graph
        {
            Orientation = Graph.Orientations.Horizontal
        };
        graph.Edges.Add(new Edge(minecraft, fabricLoader));
        graph.Edges.Add(new Edge(fabricLoader, fabricApi));
        graph.Edges.Add(new Edge(fabricApi, sodium));
        graph.Edges.Add(new Edge(fabricApi, lithium));
        graph.Edges.Add(new Edge(fabricApi, phosphor));
        return graph;
    }
}
