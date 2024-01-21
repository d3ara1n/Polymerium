namespace Polymerium.Trident.Models.Labrinth;

public struct LabrinthProjectGalleryItem
{
    public Uri Url { get; init; }
    public bool Featured { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public DateTimeOffset Created { get; init; }
    public int Ordering { get; init; }
}