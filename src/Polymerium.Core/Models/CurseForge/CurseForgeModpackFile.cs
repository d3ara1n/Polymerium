namespace Polymerium.Core.Models.CurseForge;

public struct CurseForgeModpackFile
{
    public int ProjectId { get; set; }
    public int FileId { get; set; }
    public bool Required { get; set; }
}