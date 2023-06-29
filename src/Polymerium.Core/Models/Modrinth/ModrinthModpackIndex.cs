using Newtonsoft.Json;
using Polymerium.Core.Models.Modrinth.Converters;
using System.Collections.Generic;

namespace Polymerium.Core.Models.Modrinth;

public struct ModrinthModpackIndex
{
    public uint FormatVersion { get; set; }
    public string Game { get; set; }
    public string VersionId { get; set; }
    public string Name { get; set; }
    public string Summary { get; set; }
    public IEnumerable<ModrinthModpackFile> Files { get; set; }

    [JsonConverter(typeof(ModpackDependencyConverter))]
    public IEnumerable<ModrinthModpackDependency> Dependencies { get; set; }
}
