using TridentCore.Abstractions.Adapters;

namespace Polymerium.Avalonia.Models;

public class MigrateLauncherKindModel(LauncherKind kind, string? defaultDirectory)
{
    public LauncherKind Kind { get; } = kind;
    public string? DefaultDirectory { get; } = defaultDirectory;
}
