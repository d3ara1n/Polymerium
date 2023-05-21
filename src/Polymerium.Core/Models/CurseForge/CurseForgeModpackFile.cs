namespace Polymerium.Core.Models.CurseForge;

public struct CurseForgeModpackFile
{
    public uint ProjectId { get; set; }
    public uint FileId { get; set; }
    public bool Required { get; set; }
}