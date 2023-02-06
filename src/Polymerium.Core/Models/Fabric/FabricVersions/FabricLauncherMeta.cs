using Newtonsoft.Json;
using Polymerium.Core.Models.Fabric.Converters;

namespace Polymerium.Core.Models.Fabric.FabricVersions;

public struct FabricLauncherMeta
{
    public uint Version { get; set; }
    public FabricLauncherMetaLibraries Libraries { get; set; }

    [JsonConverter(typeof(LauncherMetaMainClassConverter))]
    public FabricLauncherMetaMainClass MainClass { get; set; }
}