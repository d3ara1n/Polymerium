namespace Polymerium.Trident.Models.CurseForgeApi;

public record FileInfo(
    uint Id,
    uint GameId,
    uint ModId,
    bool IsAvailable,
    string DisplayName,
    string FileName,
    FileInfo.FileReleaseType ReleaseType,
    FileInfo.FileStatusStatus FileStatus,
    IReadOnlyList<FileInfo.FileHash> Hashes,
    DateTimeOffset FileDate,
    ulong FileLength,
    ulong DownloadCount,
    ulong? FileSizeOnDisk,
    Uri? DownloadUrl,
    IReadOnlyList<string> GameVersions,
    IReadOnlyList<SortableGameVersionModel> SortableGameVersions,
    IReadOnlyList<FileInfo.FileDependency> Dependencies,
    bool? ExposeAsAlternative,
    uint? ParentProjectFileId,
    uint? AlternativeFileId,
    bool? IsServerPack,
    uint? ServerPackFileId,
    bool? IsEarlyAccessContent,
    DateTimeOffset? EarlyAccessEndDate,
    ulong FileFingerprint,
    IReadOnlyList<FileInfo.FileModule> Modules)
{
    #region FileReleaseType enum

    public enum FileReleaseType { Release = 1, Beta, Alpha }

    #endregion

    #region FileStatusStatus enum

    public enum FileStatusStatus
    {
        Processing = 1,
        ChangesRequired,
        UnderReview,
        Approved,
        Rejected,
        MalwareDetected,
        Deleted,
        Archived,
        Testing,
        Released,
        ReadyForReview,
        Deprecated,
        Baking,
        AwaitingPublishing,
        FailedPublishing
    }

    #endregion

    #region Nested type: FileDependency

    public record FileDependency(uint ModId, FileDependency.FileRelationType RelationType)
    {
        #region FileRelationType enum

        public enum FileRelationType
        {
            EmbeddedLibrary = 1, OptionalDependency, RequiredDependency, Tool, Incompatible, Include
        }

        #endregion
    }

    #endregion

    #region Nested type: FileHash

    public record FileHash(string Value, FileHash.HashAlgo Algo)
    {
        #region HashAlgo enum

        public enum HashAlgo { Sha1 = 1, Md5 }

        #endregion
    }

    #endregion

    #region Nested type: FileModule

    public record FileModule(string Name, ulong Fingerprint);

    #endregion
}