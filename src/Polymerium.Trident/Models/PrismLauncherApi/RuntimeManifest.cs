namespace Polymerium.Trident.Models.PrismLauncherApi;

public record RuntimeManifest(
    int FormatVersion,
    string Name,
    DateTimeOffset ReleaseTime,
    IReadOnlyList<RuntimeManifest.Runtime> Runtimes)
{
    #region Nested type: Runtime

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
        #region Nested type: ChecksumData

        public record ChecksumData(string Hash, string Type);

        #endregion

        #region Nested type: VersionData

        public record VersionData(uint Major, uint Minor, uint Security);

        #endregion
    }

    #endregion
}