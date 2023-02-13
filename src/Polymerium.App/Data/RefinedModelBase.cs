using System;
using Newtonsoft.Json;

namespace Polymerium.App.Data;

public abstract class RefinedModelBase<T>
{
    private static readonly JsonSerializerSettings serializerSettings = new();

    [JsonIgnore] public abstract Uri Location { get; }

    [JsonIgnore] public virtual JsonSerializerSettings SerializerSettings { get; } = serializerSettings;

    public abstract T Extract();

    public abstract void Apply(T data);
}
