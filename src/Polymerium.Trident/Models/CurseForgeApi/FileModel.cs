﻿namespace Polymerium.Trident.Models.CurseForgeApi;

public record FileModel(
    uint Id,
    uint GameId,
    uint ModId,
    bool IsAvailable,
    string DisplayName,
    string FileName,
    FileModel.FileReleaseType ReleaseType,
    FileModel.FileStatusStatus FileStatus,
    IReadOnlyList<FileModel.FileHash> Hashes,
    DateTimeOffset FileDate,
    ulong FileLength,
    ulong DownloadCount,
    ulong? FileSizeOnDisk,
    Uri DownloadUrl,
    IReadOnlyList<string> GameVersions,
    IReadOnlyList<SortableGameVersionModel> SortableGameVersions,
    IReadOnlyList<FileModel.FileDependency> Dependencies,
    bool? ExposeAsAlternative,
    uint? ParentProjectFileId,
    uint? AlternativeFileId,
    bool? IsServerPack,
    uint? ServerPackFileId,
    bool? IsEarlyAccessContent,
    DateTimeOffset? EarlyAccessEndDate,
    ulong FileFingerprint,
    IReadOnlyList<FileModel.FileModule> Modules)
{
    public enum FileReleaseType
    {
        Release = 1,
        Beta,
        Alpha
    }

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

    public record FileHash(string Value, FileHash.HashAlgo Algo)
    {
        public enum HashAlgo
        {
            Sha1 = 1,
            Md5
        }
    }

    public record FileDependency(uint ModId, FileDependency.FileRelationType RelationType)
    {
        public enum FileRelationType
        {
            EmbeddedLibrary = 1,
            OptionalDependency,
            RequiredDependency,
            Tool,
            Incompatible,
            Include
        }
    }

    public record FileModule(string Name, ulong Fingerprint);
}