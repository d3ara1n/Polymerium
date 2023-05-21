using System.Collections.Generic;

namespace Polymerium.Core.Models.Fabric.FabricVersions;

public struct FabricLauncherMetaLibraries
{
    public IEnumerable<FabricLibraryItem> Common { get; set; }
    public IEnumerable<FabricLibraryItem> Client { get; set; }
    public IEnumerable<FabricLibraryItem> Server { get; set; }
}