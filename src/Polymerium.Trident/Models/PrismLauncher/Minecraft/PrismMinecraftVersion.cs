using System.Text.Json.Serialization;

namespace Polymerium.Trident.Models.PrismLauncher.Minecraft;

public struct PrismMinecraftVersion
{
    [JsonPropertyName("+traits")] public string[]? Traits { get; init; }
    public PrismMinecraftVersionAssetIndex? AssetIndex { get; init; }
    public uint[]? CompatibleJavaMajors { get; init; }
    public int FormatVersion { get; init; }
    public PrismMinecraftVersionLibrary[]? Libraries { get; init; }
    public string? MainClass { get; init; }
    public PrismMinecraftVersionLibrary? MainJar { get; init; }
    public string? MinecraftArguments { get; init; }
    public string Name { get; init; }
    public int Order { get; init; }
    public DateTimeOffset ReleaseTime { get; init; }
    public PrismMinecraftRequirement[] Requires { get; init; }
    public PrismMinecraftReleaseType? Type { get; init; }
    public string Uid { get; init; }
    public string Version { get; init; }
}