using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Polymerium.Core.Models.Modrinth.Labrinth;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public struct LabrinthVersion
{
    public string Name { get; set; }
    public string VersionNumber { get; set; }
    public string Changelog { get; set; }
    public object Dependencies { get; set; }
    public IEnumerable<string> GameVersions { get; set; }
    public IEnumerable<string> Loaders { get; set; }
    public bool Featured { get; set; }
    public string Status { get; set; }
    public string RequestedStatus { get; set; }
    public string Id { get; set; }
    public string ProjectId { get; set; }
    public string AuthorId { get; set; }
    public string DatePublished { get; set; }
    public long Downloads { get; set; }
    public Uri? ChangelogUrl { get; set; }
    public IEnumerable<LabrinthVersionFile> Files { get; set; }
}