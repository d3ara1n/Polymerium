using System.Text.Json.Serialization;

namespace Polymerium.Trident.Models.PrismLauncher;

public struct PrismRequirement
{
    [JsonPropertyName("Suggests")] public string? Suggest { get; init; }
    [JsonPropertyName("Equals")] public string? Equal { get; init; }
    public string Uid { get; init; }
}