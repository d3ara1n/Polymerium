using System.Collections.Generic;

namespace Polymerium.Core.Models.CurseForge;

public struct CurseForgeModpackIndex
{
    public CurseForgeModpackMinecraft Minecraft { get; set; }
    public string ManifestType { get; set; }
    public int ManifestVersion { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string Author { get; set; }
    public IEnumerable<CurseForgeModpackFile> Files { get; set; }
    public string Overrides { get; set; }
}