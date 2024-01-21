namespace Polymerium.Trident.Models.CurseForge;

public struct CurseForgeModpackManifest
{
    public CurseForgeModpackManifestMinecraft Minecraft { get; init; }
    public string ManifestType { get; init; }
    public int ManifestVersion { get; init; }
    public string Name { get; init; }
    public string Version { get; init; }
    public string Author { get; init; }
    public string Overrides { get; init; }
    public IEnumerable<CurseForgeModpackManifestFile> Files { get; init; }
}