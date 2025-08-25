namespace Polymerium.Trident.Models.PrismLauncherApi
{
    public readonly record struct RuntimeManifest(
        int FormatVersion,
        string Name,
        DateTimeOffset ReleaseTime,
        IReadOnlyList<RuntimeManifest.Runtime> Runtimes)
    {
        #region Nested type: Runtime

        public readonly record struct Runtime(
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

            public readonly record struct ChecksumData(string Hash, string Type);

            #endregion

            #region Nested type: VersionData

            public readonly record struct VersionData(uint Major, uint Minor, uint Security);

            #endregion
        }

        #endregion
    }
}
