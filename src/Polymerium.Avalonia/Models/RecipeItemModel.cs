using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Facilities;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Pref;
namespace Polymerium.Avalonia.Models;

public sealed partial class RecipeItemModel(string recipeId, string label, string? ns, string projectId) : ModelBase
{
    public string RecipeId { get; } = recipeId;
    public string Label { get; set; } = label;
    public string? Namespace { get; set; } = ns;
    public string ProjectId { get; set; } = projectId;
    public ProjectIdentifier Identifier { get; } = new(label, ns, projectId);

    [ObservableProperty]
    public partial string? Note { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<string> Tags { get; set; } = [];

    [ObservableProperty]
    public partial Project? Info { get; set; }

    [ObservableProperty]
    public partial bool IsLoaded { get; set; }
}
