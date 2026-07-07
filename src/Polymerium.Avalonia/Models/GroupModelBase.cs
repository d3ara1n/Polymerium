using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Utilities;

namespace Polymerium.Avalonia.Models;

public abstract partial class GroupModelBase : ModelBase
{
    public required PackageSourceHelper.Kind Kind { get; init; }

    public required string? Source { get; init; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; } = true;

    public virtual bool RequireGuideLine => true;
}
