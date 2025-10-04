namespace Polymerium.App.Models;

public class AssetModLoaderFilter
{
    public required string Label { get; init; }
    public ModLoaderKind? Loader { get; init; }
}
