using Polymerium.Core.Models.Fabric.FabricVersions;

namespace Polymerium.Core.Models.Fabric;

public struct FabricVersion
{
    public FabricLoader Loader { get; set; }

    public FabricIntermediary Intermediary { get; set; }

    // 没发现有什么用
    public FabricIntermediary? Hashed { get; set; }
    public FabricLauncherMeta LauncherMeta { get; set; }
}
