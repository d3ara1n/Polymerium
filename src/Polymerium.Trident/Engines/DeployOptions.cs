namespace Polymerium.Trident.Engines;

public class DeployOptions(string? javaHomeOverride = null)
{
    public string? JavaHomeOverride => javaHomeOverride;

    // TODO: Persistent & Snapshots
}