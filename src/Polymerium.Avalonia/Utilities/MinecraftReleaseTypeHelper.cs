using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Utilities;

public static class MinecraftReleaseTypeHelper
{
    // NOTE: 未知 type（Prism 后续可能新增）fallback 到 Release，避免展示层因解析失败而崩溃。
    public static MinecraftReleaseType FromComponentType(string? type) => type switch
    {
        "release" => MinecraftReleaseType.Release,
        "snapshot" => MinecraftReleaseType.Snapshot,
        "experiment" => MinecraftReleaseType.Experiment,
        "old_snapshot" => MinecraftReleaseType.OldSnapshot,
        "old_beta" => MinecraftReleaseType.OldBeta,
        "old_alpha" => MinecraftReleaseType.OldAlpha,
        _ => MinecraftReleaseType.Release
    };
}
