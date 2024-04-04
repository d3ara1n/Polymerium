namespace Polymerium.Trident.Models.Labrinth;

public struct LabrinthVersionDependency
{
    public string? VersionId { get; set; }
    public string? ProjectId { get; set; }
    public string? FileName { get; set; }
    public string DependencyType { get; set; }
}