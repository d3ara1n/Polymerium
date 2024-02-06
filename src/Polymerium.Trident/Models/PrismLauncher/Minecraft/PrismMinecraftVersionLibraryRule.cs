namespace Polymerium.Trident.Models.PrismLauncher.Minecraft;

public struct PrismMinecraftVersionLibraryRule
{
    public PrismMinecraftVersionLibraryRuleAction Action { get; init; }
    public IDictionary<string, bool> Os { get; init; }
}