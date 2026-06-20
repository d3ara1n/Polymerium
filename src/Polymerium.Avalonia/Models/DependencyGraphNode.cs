using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Facilities;
using TridentCore.Abstractions.Repositories.Resources;

namespace Polymerium.Avalonia.Models;

/// <summary>
///     依赖图中的一个已安装包节点，携带展示铭牌与详情面板所需的包元数据，
///     以及图上的选中状态（由 ViewModel 驱动，控件据此切换样式）。
/// </summary>
public sealed partial class DependencyGraphNode(
    string key,
    string label,
    string projectName,
    string? ns,
    string projectId,
    ResourceKind kind,
    string? author,
    Uri? thumbnail,
    ReleaseType releaseType) : ModelBase
{
    public string Key { get; } = key;
    public string Label { get; } = label;
    public string ProjectName { get; } = projectName;
    public string? Namespace { get; } = ns;
    public string ProjectId { get; } = projectId;
    public ResourceKind Kind { get; } = kind;
    public string? Author { get; } = author;
    public Uri? Thumbnail { get; } = thumbnail;
    public ReleaseType ReleaseType { get; } = releaseType;

    /// <summary>是否在图上被选中。由 ModalModel.SelectNode 维护，控件据此驱动 :selected 样式。</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
