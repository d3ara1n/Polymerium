using Microsoft.Extensions.Options;
using Polymerium.Trident.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Services.Profiles;
public class ProfileConverter : JsonConverter<Attachment>
{
    private static readonly JsonSerializerOptions COMPATIBLE_OPTIONS = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    public override Attachment? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && AurlHelper.TryParseAurl(reader.GetString()!, out var attachment))
        {
            return attachment;
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {

            return JsonSerializer.Deserialize<Attachment>(ref reader, COMPATIBLE_OPTIONS);
        }
        throw new JsonException();

    }
    public override void Write(Utf8JsonWriter writer, Attachment value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(AurlHelper.MakeAurl(value));
    }
}