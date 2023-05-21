using System;
using System.Collections.Generic;
using System.Linq;

namespace Polymerium.Core.Models.CurseForge.Eternal;

public struct EternalModFile
{
    public uint Id { get; set; }
    public uint GameId { get; set; }
    public uint ModId { get; set; }
    public bool IsAvailable { get; set; }
    public string DisplayName { get; set; }
    public string FileName { get; set; }
    public int ReleaseType { get; set; }
    public int FileStatus { get; set; }
    public IEnumerable<EternalModFileHash> Hashes { get; set; }
    public DateTimeOffset FileDate { get; set; }
    public ulong FileLength { get; set; }
    public int DownloadCount { get; set; }
    public Uri? DownloadUrl { get; set; }
    public IEnumerable<string> GameVersions { get; set; }
    public object SortableGameVersions { get; set; }
    public IEnumerable<EternalModFileDependency> Dependencies { get; set; }
    public bool ExposeAsAlternative { get; set; }
    public int ParentProjectFileId { get; set; }
    public int AlternateFileId { get; set; }
    public bool IsServerPack { get; set; }
    public int ServerPackFileId { get; set; }
    public long FileFingerprint { get; set; }
    public object Modules { get; set; }

    public Uri ExtractDownloadUrl()
    {
        return DownloadUrl
               ?? new Uri($"https://edge.forgecdn.net/files/{Id / 1000}/{Id % 1000}/{FileName}");
    }

    public string? ExtractSha1()
    {
        return Hashes.Any(x => x.Algo == 1) ? Hashes.First(x => x.Algo == 1).Value : null;
    }
}