using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Polymerium.Core.Models.Modrinth.Converters;

public class ModpackDependencyConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        var obj = JObject.Load(reader);
        var props = obj.Properties();
        return props.Select(x =>
        {
            var key = x.Name;
            var value = x.Value.Value<string>();
            return new ModrinthModpackDependency
            {
                Id = key,
                Version = value ?? string.Empty
            };
        });
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(ModrinthModpackDependency) == objectType;
    }
}