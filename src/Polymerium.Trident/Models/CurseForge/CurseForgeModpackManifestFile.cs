namespace Polymerium.Trident.Models.CurseForge;

public struct CurseForgeModpackManifestFile
{
    public uint ProjectId { get; set; }
    public uint FileId { get; set; }
    public bool Required { get; set; }
}