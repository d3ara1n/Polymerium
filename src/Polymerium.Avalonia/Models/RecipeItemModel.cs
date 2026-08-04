using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Facilities;
using TridentCore.Abstractions.Repositories.Resources;

namespace Polymerium.Avalonia.Models;

public sealed partial class RecipeItemModel(string id, string label, string? ns, string projectId) : ModelBase
{
    public string Id { get; } = id;
    public string Label { get; set; } = label;
    public string? Namespace { get; set; } = ns;
    public string ProjectId { get; set; } = projectId;

    [ObservableProperty]
    public partial string? Note { get; set; }

    [ObservableProperty]
    public partial Project? Info { get; set; }

    [ObservableProperty]
    public partial bool IsLoaded { get; set; }
}
