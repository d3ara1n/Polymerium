using Polymerium.Trident.Models.Minecraft.Converters;
using System.Text.Json.Serialization;

namespace Polymerium.Trident.Models.Minecraft;

public struct MinecraftVersionArgument
{
    public MinecraftVersionRule[] Rules { get; init; }

    [JsonConverter(typeof(MinecraftVersionArgumentValueConverter))]
    public string[] Value { get; init; }
}