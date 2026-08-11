using System;

namespace Polymerium.Avalonia.Models;

public class AssetModeMetadataModel
{
    // NOTE: Mod 元数据（读自 mod.jar 的 fabric.mod.json 或 mods.toml）。

    public string? ModId { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Description { get; set; }
    public string[]? Authors { get; set; }
    public Uri? Homepage { get; set; }
    public string? License { get; set; }
    public string? LogoFile { get; set; }
    public ModLoaderKind? LoaderType { get; set; }
}
