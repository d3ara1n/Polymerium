using System;

namespace Polymerium.App.Models;

public class AssetModeMetadataModel
{
    // Mod 元数据类（从 mod.jar 中的 fabric.mod.json 或 mods.toml 读取）

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
