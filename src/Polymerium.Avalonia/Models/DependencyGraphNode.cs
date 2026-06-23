using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Facilities;
using TridentCore.Abstractions.Repositories.Resources;

namespace Polymerium.Avalonia.Models;

public sealed partial class DependencyGraphNode(
    string key,
    string label,
    string projectName,
    string? ns,
    string projectId,
    ResourceKind kind,
    string? author,
    Bitmap thumbnail,
    ReleaseType releaseType) : ModelBase
{
    public string Key { get; } = key;
    public string Label { get; } = label;
    public string ProjectName { get; } = projectName;
    public string? Namespace { get; } = ns;
    public string ProjectId { get; } = projectId;
    public ResourceKind Kind { get; } = kind;
    public string? Author { get; } = author;
    public Bitmap Thumbnail { get; } = thumbnail;
    public ReleaseType ReleaseType { get; } = releaseType;

    public bool IsMissing { get; set; }

    public int DependencyCount { get; set; }

    public int DependentCount { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
