using TridentCore.Abstractions.Adapters;
using TridentCore.Abstractions.Utilities;

namespace Polymerium.Avalonia.Models;

public class MigrateInstanceRow(LauncherInstance instance)
{
    public LauncherInstance Instance { get; } = instance;

    public bool IsSelected { get; set; }

    public string Key => Instance.Key;
    public string Name => Instance.Name ?? Instance.Key;
    public string? MinecraftVersion => Instance.MinecraftVersion;
    public string? Loader => Instance.Loader;

    // "Fabric 0.15.0" when a loader is set, null for vanilla. Atomic — the view composes this with the
    // version so multiple display sites share it instead of each needing its own pre-joined property.
    public string? LoaderDisplay => Instance.Loader is null ? null : LoaderHelper.ToDisplayLabel(Instance.Loader);

    public bool HasLoader => Instance.Loader is not null;

    public bool IsCorrupt => Instance.IsCorrupt;
    public string? CorruptReason => Instance.CorruptReason;
}
