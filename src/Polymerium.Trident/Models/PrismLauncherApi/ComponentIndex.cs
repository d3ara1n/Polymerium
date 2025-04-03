using System.Text.Json.Serialization;

namespace Polymerium.Trident.Models.PrismLauncherApi;

public record ComponentIndex(
    int FormatVersion,
    string Name,
    string Uid,
    IReadOnlyList<ComponentIndex.ComponentVersion> Versions)
{
    #region Nested type: ComponentVersion

    public record ComponentVersion(
        bool Recommended,
        DateTimeOffset ReleaseTime,
        IReadOnlyList<ComponentVersion.VersionRequirement> Requires,
        string Sha256,
        string Type,
        string Version)
    {
        #region Nested type: VersionRequirement

        public record VersionRequirement(
            [property: JsonPropertyName("suggests")]
            string? Suggest,
            [property: JsonPropertyName("equals")] string? Equal,
            string Uid);

        #endregion
    }

    #endregion
}