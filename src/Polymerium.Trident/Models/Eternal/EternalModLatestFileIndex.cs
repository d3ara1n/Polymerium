namespace Polymerium.Trident.Models.Eternal;

public struct EternalModLatestFileIndex
{
    public string GameVersion { get; set; }
    public uint FileId { get; set; }
    public string FileName { get; set; }
    public int ReleaseType { get; set; }

    public uint? GameVersionTypeId { get; set; }

    // 0=Any
    // 1=Forge
    // 2=Cauldron
    // 3=LiteLoader
    // 4=Fabric
    // 5=Quilt
    public uint? ModLoader { get; set; }
}