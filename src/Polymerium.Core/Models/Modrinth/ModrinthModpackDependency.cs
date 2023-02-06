namespace Polymerium.Core.Models.Modrinth;

public struct ModrinthModpackDependency
{
    // minecraft, forge, fabric-loader, quilt-loader
    public string Id { get; set; }
    public string Version { get; set; }
}