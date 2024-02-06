namespace Polymerium.Trident.Models.Labrinth;

public struct LabrinthProject
{
    public string Id { get; init; }
    public string Slug { get; init; }
    public string Title { get; init; }
    public string Summary { get; init; }
    public string Description { get; init; }
    public string[] Categories { get; init; }
    public string[] AdditionalCategories { get; init; }
    public string[] ProjectTypes { get; init; }
    public string TeamId { get; init; }
    public string Name { get; init; }
    public string[] Games { get; init; }
    public DateTimeOffset Published { get; init; }
    public DateTimeOffset Updated { get; init; }
    public DateTimeOffset Approved { get; init; }
    public DateTimeOffset Queued { get; init; }
    public string Status { get; init; }
    public uint Downloads { get; init; }
    public uint Followers { get; init; }
    public string[] Loaders { get; init; }
    public string[] Versions { get; init; }
    public Uri IconUrl { get; init; }
    public LabrinthProjectGalleryItem[] Gallery { get; init; }
}