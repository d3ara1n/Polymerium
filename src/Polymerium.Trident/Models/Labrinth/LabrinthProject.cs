namespace Polymerium.Trident.Models.Labrinth;

public struct LabrinthProject
{
    public string Id { get; set; }
    public string Slug { get; set; }
    public string Title { get; set; }
    public string Summary { get; set; }
    public string Description { get; set; }
    public IEnumerable<string> Categories { get; set; }
    public IEnumerable<string> AdditionalCategories { get; set; }
    public IEnumerable<string> ProjectTypes { get; set; }
    public string TeamId { get; set; }
    public string Name { get; set; }
    public IEnumerable<string> Games { get; set; }
    public DateTimeOffset Published { get; set; }
    public DateTimeOffset Updated { get; set; }
    public DateTimeOffset Approved { get; set; }
    public DateTimeOffset Queued { get; set; }
    public string Status { get; set; }
    public uint Downloads { get; set; }
    public uint Followers { get; set; }
    public IEnumerable<string> Loaders { get; set; }
    public IEnumerable<string> Versions { get; set; }
    public Uri IconUrl { get; set; }
    public IEnumerable<LabrinthProjectGalleryItem> Gallery { get; set; }
}