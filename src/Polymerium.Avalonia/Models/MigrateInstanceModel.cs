using TridentCore.Abstractions.Adapters;

namespace Polymerium.Avalonia.Models;

public class MigrateInstanceModel(LauncherInstance instance)
{
    public LauncherInstance Instance { get; } = instance;

    public bool IsSelected { get; set; }
}
