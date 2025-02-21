namespace Polymerium.Trident.Models.PrismLauncherApi;

public record ComponentIndex(
    uint FormatVersion,
    string Name,
    string Uid,
    IReadOnlyList<ComponentIndex.ComponentVersion> Versions)
{
    public record ComponentVersion(
        bool Recommended,
        DateTimeOffset ReleaseTime,
        IReadOnlyList<ComponentVersion.VersionRequirement> Requires,
        string Sha256,
        string Type,
        string Version)
    {
        public record VersionRequirement(string Suggests, string Uid);
    }
}