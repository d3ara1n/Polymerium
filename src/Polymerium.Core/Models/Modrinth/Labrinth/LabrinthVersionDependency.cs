using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Polymerium.Core.Models.Modrinth.Labrinth;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public struct LabrinthVersionDependency
{
    public string? VersionId { get; set; }
    public string? ProjectId { get; set; }
    public string? FileName { get; set; }
    public string DependencyType { get; set; }
}