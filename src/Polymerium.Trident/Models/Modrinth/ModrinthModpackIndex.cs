namespace Polymerium.Trident.Models.Modrinth;

public struct ModrinthModpackIndex
{
    public uint FormatVersion { get; set; }
    public string Game { get; set; }
    public string VersionId { get; set; }
    public string Name { get; set; }
    public string Summary { get; set; }
    public IEnumerable<ModrinthModpackFile> Files { get; set; }
    public IDictionary<string, string> Dependencies { get; set; }
}