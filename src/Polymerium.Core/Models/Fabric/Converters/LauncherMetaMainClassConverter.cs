using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polymerium.Core.Models.Fabric.FabricVersions;

namespace Polymerium.Core.Models.Fabric.Converters;

public class LauncherMetaMainClassConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        var token = JToken.Load(reader);
        if (token.Type == JTokenType.String)
        {
            var mainClass = token.ToObject<string>()!;
            return new FabricLauncherMetaMainClass
            {
                Client = mainClass,
                Server = mainClass
            };
        }

        return token.ToObject<FabricLauncherMetaMainClass>();
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(FabricLauncherMetaMainClass) == objectType;
    }
}