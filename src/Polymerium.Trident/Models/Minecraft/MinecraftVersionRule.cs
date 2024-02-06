using System.Text.Json.Serialization;
using Polymerium.Trident.Models.Minecraft.Converters;

namespace Polymerium.Trident.Models.Minecraft;

[JsonConverter(typeof(MinecraftVersionRuleConverter))]
public struct MinecraftVersionRule
{
    public MinecraftVersionRuleAction Action { get; init; }
    public IDictionary<string, bool> Features { get; init; }
    public IDictionary<string, bool> Os { get; init; }
}