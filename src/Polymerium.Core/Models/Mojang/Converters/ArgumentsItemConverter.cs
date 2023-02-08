using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polymerium.Core.Models.Mojang.Indexes;

namespace Polymerium.Core.Models.Mojang.Converters;

internal class ArgumentsItemConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(ArgumentsItem);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        var token = JToken.Load(reader);
        if (token.Type == JTokenType.String)
            return new ArgumentsItem
            {
                Values = new[] { token.ToObject<string>()! }
            };

        if (token.Type == JTokenType.Object)
        {
            var obj = token as JObject;
            var item = new ArgumentsItem();
            var value = obj!.GetValue("value");
            if (value is JArray values)
                item.Values = values.Values().Select(x =>
                    x.Type == JTokenType.String ? x.Value<string>()! : throw new InvalidCastException(x.ToString()));
            else if (value!.Type == JTokenType.String)
                item.Values = new[] { value.Value<string>()! };
            else
                throw new InvalidCastException(value.ToString());
            item.Rules = obj.Value<JArray>("rules")!.ToObject<IEnumerable<Rule>>()!;
            return item;
        }

        throw new InvalidCastException(token.ToString());
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}