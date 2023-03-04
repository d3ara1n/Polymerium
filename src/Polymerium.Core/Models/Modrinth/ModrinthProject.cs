using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Polymerium.Core.Models.Modrinth;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public struct ModrinthProject
{
    public string? Id { get; set; }
    public string Title { get; set; }
    public string Slug { get; set; }
    public string ProjectType { get; set; }
    public string Team { get; set; }
    public string Description { get; set; }
    public string Body { get; set; }
    public Uri? BodyUrl { get; set; }
    public DateTimeOffset Published { get; set; }
    public DateTimeOffset Updated { get; set; }
    public DateTimeOffset Approved { get; set; }
    public string Status { get; set; }
    public object? RequestedStatus { get; set; }
    public object? ModeratorMessage { get; set; }
    public object? License { get; set; }
    public string ClientSide { get; set; }
    public string ServerSide { get; set; }
    public uint Downloads { get; set; }
    public uint Followers { get; set; }
    public IEnumerable<string> Categories { get; set; }
    public IEnumerable<string> AdditionalCategories { get; set; }
    public IEnumerable<string> GameVersions { get; set; }
    public IEnumerable<string> Loaders { get; set; }
    public IEnumerable<string> Versions { get; set; }
    public Uri IconUrl { get; set; }
    public Uri? IssuesUrl { get; set; }
    public Uri? SourceUrl { get; set; }
    public Uri? WikiUrl { get; set; }
    public Uri? DiscordUrl { get; set; }
    public IEnumerable<object> DonationUrls { get; set; }
    public IEnumerable<object> Gallery { get; set; }
    public object? FlameAnvilProject { get; set; }
    public object? FlameAnvilUser { get; set; }
    public uint? Color { get; set; }
}