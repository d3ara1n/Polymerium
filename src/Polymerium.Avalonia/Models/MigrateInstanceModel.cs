using CommunityToolkit.Mvvm.ComponentModel;
using TridentCore.Abstractions.Adapters;

namespace Polymerium.Avalonia.Models;

public partial class MigrateInstanceModel(LauncherInstance instance) : ObservableObject
{
    public LauncherInstance Instance { get; } = instance;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
