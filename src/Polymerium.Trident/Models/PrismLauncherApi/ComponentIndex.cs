using System.Text.Json.Serialization;

namespace Polymerium.Trident.Models.PrismLauncherApi;

public readonly record struct ComponentIndex(
    int FormatVersion,
    string Name,
    string Uid,
    IReadOnlyList<ComponentIndex.ComponentVersion> Versions)
{
    #region Nested type: ComponentVersion

    public readonly record struct ComponentVersion(
        bool Recommended,
        DateTimeOffset ReleaseTime,
        IReadOnlyList<ComponentVersion.VersionRequirement> Requires,
        string Sha256,
        string Type,
        string Version)
    {
        #region Nested type: VersionRequirement

        public readonly record struct VersionRequirement(
            [property: JsonPropertyName("suggests")]
            string? Suggest,
            [property: JsonPropertyName("equals")] string? Equal,
            string Uid);

        #endregion
    }

    #endregion
}