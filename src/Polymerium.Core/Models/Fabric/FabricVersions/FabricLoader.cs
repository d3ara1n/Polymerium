namespace Polymerium.Core.Models.Fabric.FabricVersions;

public struct FabricLoader
{
    public string Separator { get; set; }
    public uint Build { get; set; }
    public string Maven { get; set; }
    public string Version { get; set; }
    public bool Stable { get; set; }
}
