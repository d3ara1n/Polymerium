using System.Text.Json.Serialization;

namespace Polymerium.Trident.Models.PrismLauncher
{
    public struct PrismVersion
    {
        [JsonPropertyName("+traits")] public string[]? Traits { get; init; }
        public PrismMinecraftVersionAssetIndex? AssetIndex { get; init; }
        public uint[]? CompatibleJavaMajors { get; init; }
        public int FormatVersion { get; init; }
        public PrismVersionLibrary[]? Libraries { get; init; }
        public PrismVersionLibrary[]? MavenFiles { get; init; }
        public string? MainClass { get; init; }
        public PrismVersionLibrary? MainJar { get; init; }
        public string? MinecraftArguments { get; init; }
        public string Name { get; init; }
        public int Order { get; init; }
        public DateTimeOffset ReleaseTime { get; init; }
        public PrismRequirement[] Requires { get; init; }
        public PrismReleaseType? Type { get; init; }
        public string Uid { get; init; }
        public string Version { get; init; }
    }
}