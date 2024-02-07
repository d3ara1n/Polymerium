namespace Polymerium.Trident.Models.PrismLauncher.Minecraft;

public struct PrismMinecraftVersionLibraryRule
{
    public PrismMinecraftVersionLibraryRuleAction Action { get; init; }

    /// Typically name, arch, version
    public IDictionary<string, string>? Os { get; init; }
}