namespace Polymerium.Trident.Models.Eternal;

public struct EternalProjectLatestFile
{
    public uint Id { get; set; }
    public uint GameId { get; set; }
    public uint ModId { get; set; }
    public bool IsAvailable { get; set; }
    public string DisplayName { get; set; }
    public string FileName { get; set; }
    public int ReleaseType { get; set; }
    public int FileStatus { get; set; }
    public IEnumerable<EternalFileHash> Hashes { get; set; }
    public DateTimeOffset FileDate { get; set; }
    public long FileLength { get; set; }
    public long DownloadCount { get; set; }
    public Uri DownloadUrl { get; set; }
    public IEnumerable<string> GameVersions { get; set; }
    public object SortableGameVersions { get; set; }
    public IEnumerable<EternalDependency> Dependencies { get; set; }
    public bool ExposeAsAlternative { get; set; }
    public uint ParentProjectFileId { get; set; }
    public uint AlternateFileId { get; set; }
    public bool IsServerPack { get; set; }
    public uint ServerPackFileId { get; set; }
    public long FileFingerprint { get; set; }
    public object Module { get; set; }
}