using System;
using System.Collections.Generic;

namespace Polymerium.Core.Models.CurseForge.Eternal;

public struct EternalModFile
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public int ModId { get; set; }
    public bool IsAvailable { get; set; }
    public string DisplayName { get; set; }
    public string FileName { get; set; }
    public int ReleaseType { get; set; }
    public int FileStatus { get; set; }
    public IEnumerable<EternalModFileHash> Hashes { get; set; }
    public DateTimeOffset FileDate { get; set; }
    public ulong FileLength { get; set; }
    public int DownloadCount { get; set; }
    public string DownloadUrl { get; set; }
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
}