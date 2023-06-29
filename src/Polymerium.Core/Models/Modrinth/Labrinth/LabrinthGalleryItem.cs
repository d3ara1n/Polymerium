using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace Polymerium.Core.Models.Modrinth.Labrinth;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public struct LabrinthGalleryItem
{
    public string Title { get; set; }
    public Uri Url { get; set; }
    public bool Featured { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset Created { get; set; }
    public int Ordering { get; set; }
}
