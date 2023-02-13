using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Polymerium.Core.Models.Mojang.Converters;

internal class AssetsIndexConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(AssetsIndex);
    }

    public override object ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer
    )
    {
        var index = JObject.Load(reader);
        var objects = index.Value<JObject>("objects")!;
        var properties = objects.Properties();
        var items = new List<AssetsIndexItem>();
        var res = new AssetsIndex { Objects = items };
        foreach (var prop in properties)
        {
            var item = new AssetsIndexItem();
            item.FileName = prop.Name;
            var value = prop.Value as JObject;
            item.Size = value!.Value<uint>("size")!;
            item.Hash = value.Value<string>("hash")!;
            items.Add(item);
        }

        return res;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
