namespace Polymerium.Trident.Models.ModrinthApi
{
    public readonly record struct VersionInfo(
        string Id,
        string ProjectId,
        string AuthorId,
        bool Featured,
        string Name,
        string VersionNumber,
        IReadOnlyList<string> ProjectTypes,
        IReadOnlyList<string> Games,
        string? Changelog,
        DateTimeOffset DatePublished,
        ulong Downloads,
        string VersionType,
        string Status,
        string? RequestedStatus,
        IReadOnlyList<VersionInfo.VersionFile> Files,
        IReadOnlyList<VersionInfo.VersionDependency> Dependencies,
        IReadOnlyList<string> Loaders,
        int? Ordering,
        bool Singleplayer,
        bool ClientAndServer,
        IReadOnlyList<string>? MrpackLoaders,
        bool ServerOnly,
        IReadOnlyList<string> GameVersions,
        bool ClientOnly)
    {
        #region Nested type: VersionDependency

        public readonly record struct VersionDependency(
            string ProjectId,
            string VersionId,
            string? FileName,
            string DependencyType);

        #endregion

        #region Nested type: VersionFile

        public record VersionFile(
            VersionFile.FileHashes Hashes,
            Uri Url,
            string Filename,
            bool Primary,
            ulong Size,
            string? FileType)
        {
            #region Nested type: FileHashes

            public record FileHashes(string Sha1, string Sha512);

            #endregion
        }

        #endregion
    }
}
