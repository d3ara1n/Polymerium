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
    public record AssetIndexEntry(string Id, string Sha1, ulong Size, ulong TotalSize, Uri Url);

    public record Library(
        Library.DownloadsEntry? Downloads,
        Library.ExtractExtry? Extract,
        string Name,
        Uri? Url,
        Library.NativesEntry? Natives,
        IReadOnlyList<Library.Rule>? Rules)
    {
        public record DownloadsEntry(
            DownloadsEntry.ArtifactEntry? Artifact,
            IDictionary<string, DownloadsEntry.ArtifactEntry> Classifiers)
        {
            public record ArtifactEntry(string Sha1, ulong Size, Uri Url);
        }

        public record ExtractExtry(IReadOnlyList<string> Exclude);

        public record NativesEntry(string? Windows, string? Linux, string? Osx);

        public record Rule(string Action, IDictionary<string, string>? Os);
    }

    public record Requirement(
        [property: JsonPropertyName("suggests")]
        string? Suggest,
        [property: JsonPropertyName("equals")] string? Equal,
        string Uid);
}