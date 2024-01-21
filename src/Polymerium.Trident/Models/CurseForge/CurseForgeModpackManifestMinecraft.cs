namespace Polymerium.Trident.Models.CurseForge;

public struct CurseForgeModpackManifestMinecraft
{
    public string Version { get; set; }
    public IEnumerable<CurseForgeModpackManifestMinecraftModLoader> ModLoaders { get; set; }
}