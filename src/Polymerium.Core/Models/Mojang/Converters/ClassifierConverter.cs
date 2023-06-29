using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polymerium.Core.Models.Mojang.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Polymerium.Core.Models.Mojang.Converters;

internal class ClassifierConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(IEnumerable<LibraryDownloadsClassifier>);
    }

    public override object ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer
    )
    {
        var obj = JObject.Load(reader);
        var props = obj.Properties();
        var list = props.Select(x =>
        {
            var inner = x.Value.ToObject<LibraryDownloadsClassifier>();
            inner.Identity = x.Name;
            return inner;
        });
        return list;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
