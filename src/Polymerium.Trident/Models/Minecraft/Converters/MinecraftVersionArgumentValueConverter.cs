using System.Text.Json;
using System.Text.Json.Serialization;

namespace Polymerium.Trident.Models.Minecraft.Converters;

public class MinecraftVersionArgumentValueConverter : JsonConverter<string[]>
{
    public override string[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return [reader.GetString()];
        if (reader.TokenType == JsonTokenType.StartArray)
            return JsonSerializer.Deserialize<string[]>(ref reader, options);
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, string[] value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}