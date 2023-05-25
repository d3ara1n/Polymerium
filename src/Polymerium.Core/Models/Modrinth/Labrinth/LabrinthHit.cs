using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Polymerium.Core.Models.Modrinth.Labrinth;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public struct LabrinthHit
{
    public string ProjectId { get; set; }
    public string ProjectType { get; set; }
    public string Slug { get; set; }
    public string Author { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public IEnumerable<string> Categories { get; set; }
    public IEnumerable<string> DisplayCategproes { get; set; }
    public IEnumerable<string> Versions { get; set; }
    public uint Downloads { get; set; }
    public uint Follows { get; set; }
    public Uri IconUrl { get; set; }
    public DateTimeOffset DateCreated { get; set; }
    public DateTimeOffset DateModified { get; set; }
    public string LatestVersion { get; set; }
    public string License { get; set; }
    public string ClientSide { get; set; }
    public string ServerSide { get; set; }
    public IEnumerable<Uri> Gallery { get; set; }
    public Uri FeaturedGallery { get; set; }
    public long? Color { get; set; }
}
