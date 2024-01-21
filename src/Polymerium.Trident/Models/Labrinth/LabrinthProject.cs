namespace Polymerium.Trident.Models.Labrinth;

public struct LabrinthProject
{
    public string Id { get; init; }
    public string Slug { get; init; }
    public string Title { get; init; }
    public string Summary { get; init; }
    public string Description { get; init; }
    public IEnumerable<string> Categories { get; init; }
    public IEnumerable<string> AdditionalCategories { get; init; }
    public IEnumerable<string> ProjectTypes { get; init; }
    public string TeamId { get; init; }
    public string Name { get; init; }
    public IEnumerable<string> Games { get; init; }
    public DateTimeOffset Published { get; init; }
    public DateTimeOffset Updated { get; init; }
    public DateTimeOffset Approved { get; init; }
    public DateTimeOffset Queued { get; init; }
    public string Status { get; init; }
    public uint Downloads { get; init; }
    public uint Followers { get; init; }
    public IEnumerable<string> Loaders { get; init; }
    public IEnumerable<string> Versions { get; init; }
    public Uri IconUrl { get; init; }
    public IEnumerable<LabrinthProjectGalleryItem> Gallery { get; init; }
}