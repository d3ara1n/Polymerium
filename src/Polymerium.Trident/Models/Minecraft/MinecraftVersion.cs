namespace Polymerium.Trident.Models.Minecraft
{
    public struct MinecraftVersion
    {
        public MinecraftVersionArguments? Arguments { get; init; }
        public MinecraftVersionAssetIndex AssetIndex { get; init; }
        public string Assets { get; init; }
        public int ComplianceLevel { get; init; }
        public MinecraftVersionDownloads Downloads { get; init; }
        public string Id { get; init; }
        public MinecraftVersionJavaVersion JavaVersion { get; init; }
        public MinecraftVersionLibrary[] Libraries { get; init; }
        public MinecraftVersionLogging Logging { get; init; }
        public string MainClass { get; init; }
        public string? MinecraftArguments { get; init; }
        public int MinimumLauncherVersion { get; init; }
        public DateTimeOffset ReleaseTime { get; init; }
        public DateTimeOffset Time { get; init; }
        public MinecraftReleaseType Type { get; init; }
    }
}