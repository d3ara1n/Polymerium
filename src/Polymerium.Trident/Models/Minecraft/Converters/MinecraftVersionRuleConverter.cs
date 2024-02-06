using System.Text.Json;
using System.Text.Json.Serialization;

namespace Polymerium.Trident.Models.Minecraft.Converters;

public class MinecraftVersionRuleConverter : JsonConverter<MinecraftVersionArgument>
{
    public override MinecraftVersionArgument Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return new MinecraftVersionArgument
            {
                Rules =
                [
                    new MinecraftVersionRule
                    {
                        Action = MinecraftVersionRuleAction.Allow,
                        Features = new Dictionary<string, bool>(),
                        Os = new Dictionary<string, bool>()
                    }
                ],
                Value = [reader.GetString()]
            };
        if (reader.TokenType == JsonTokenType.StartObject)
            return JsonSerializer.Deserialize<MinecraftVersionArgument>(ref reader, options);
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, MinecraftVersionArgument value,
        JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}