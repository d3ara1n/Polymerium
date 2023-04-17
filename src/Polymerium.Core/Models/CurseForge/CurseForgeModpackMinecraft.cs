using System.Collections.Generic;

namespace Polymerium.Core.Models.CurseForge;

public struct CurseForgeModpackMinecraft
{
    public string Version { get; set; }
    public IEnumerable<CurseForgeModpackMinecraftModLoader> ModLoaders { get; set; }
}
