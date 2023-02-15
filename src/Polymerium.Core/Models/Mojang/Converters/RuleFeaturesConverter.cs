using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polymerium.Core.Models.Mojang.Indexes;

namespace Polymerium.Core.Models.Mojang.Converters;

internal class RuleFeaturesConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(RuleFeatures);
    }

    public override object ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer
    )
    {
        var obj = JObject.Load(reader);
        var prop = obj.Properties().First();
        var features = new RuleFeatures();
        features.Key = prop.Name;
        features.Enabled = prop.Value.Type != JTokenType.Boolean || prop.Value.Value<bool>();
        return features;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}