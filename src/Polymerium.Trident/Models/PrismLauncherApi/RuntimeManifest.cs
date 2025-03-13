namespace Polymerium.Trident.Models.PrismLauncherApi;

public record RuntimeManifest(
    int FormatVersion,
    string Name,
    DateTimeOffset ReleaseTime,
    IReadOnlyList<RuntimeManifest.Runtime> Runtimes)
{
    public record Runtime(
        Runtime.ChecksumData Checksum,
        string DownloadType,
        string Name,
        string PackageType,
        DateTimeOffset ReleaseTime,
        string RuntimeOS,
        Uri Url,
        string Vendor,
        Runtime.VersionData Version)
    {
        public record ChecksumData(string Hash, string Type);

        public record VersionData(uint Major, uint Minor, uint Security);
    }
}