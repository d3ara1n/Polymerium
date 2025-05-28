using System.Text.Json.Serialization;

namespace Polymerium.Trident.Models.PrismLauncherApi;

public record Component(
    [property: JsonPropertyName("+tweakers")]
    IReadOnlyList<string>? Tweakers,
    [property: JsonPropertyName("+traits")]
    IReadOnlyList<string>? Traits,
    Component.AssetIndexEntry? AssetIndex,
    IReadOnlyList<uint>? CompatibleJavaMajors,
    int FormatVersion,
    IReadOnlyList<Component.Library>? Libraries,
    IReadOnlyList<Component.Library>? MavenFiles,
    string? MainClass,
    Component.Library? MainJar,
    string? MinecraftArguments,
    string Name,
    int Order,
    DateTimeOffset ReleaseTime,
    IReadOnlyList<Component.Requirement> Requires,
    string Type,
    string Uid,
    string Version)

{
    #region Nested type: AssetIndexEntry

    public record AssetIndexEntry(string Id, string Sha1, ulong Size, ulong TotalSize, Uri Url);

    #endregion

    #region Nested type: Library

    public record Library(
        Library.DownloadsEntry? Downloads,
        Library.ExtractExtry? Extract,
        string Name,
        Uri? Url,
        Library.NativesEntry? Natives,
        IReadOnlyList<Library.Rule>? Rules)
    {
        #region Nested type: DownloadsEntry

        public record DownloadsEntry(
            DownloadsEntry.ArtifactEntry? Artifact,
            IDictionary<string, DownloadsEntry.ArtifactEntry> Classifiers)
        {
            #region Nested type: ArtifactEntry

            public record ArtifactEntry(string Sha1, ulong Size, Uri Url);

            #endregion
        }

        #endregion

        #region Nested type: ExtractExtry

        public record ExtractExtry(IReadOnlyList<string> Exclude);

        #endregion

        #region Nested type: NativesEntry

        public record NativesEntry(string? Windows, string? Linux, string? Osx);

        #endregion

        #region Nested type: Rule

        public record Rule(string Action, IDictionary<string, string>? Os);

        #endregion
    }

    #endregion

    #region Nested type: Requirement

    public record Requirement(
        [property: JsonPropertyName("suggests")]
        string? Suggest,
        [property: JsonPropertyName("equals")] string? Equal,
        string Uid);

    #endregion
}