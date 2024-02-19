namespace Polymerium.Trident.Models.Eternal
{
    public struct EternalModLatestFileIndex
    {
        public string GameVersion { get; init; }
        public uint FileId { get; init; }
        public string FileName { get; init; }
        public int ReleaseType { get; init; }

        public uint? GameVersionTypeId { get; init; }

        // 0=Any
        // 1=Forge
        // 2=Cauldron
        // 3=LiteLoader
        // 4=Fabric
        // 5=Quilt
        public uint? ModLoader { get; init; }
    }
}