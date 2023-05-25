using System.Collections.Generic;

namespace Polymerium.Core.Models.Fabric;

public struct FabricModInfo
{
    public string Name { get; set; }
    public string Version { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public IEnumerable<string> Authors { get; set; }
}
