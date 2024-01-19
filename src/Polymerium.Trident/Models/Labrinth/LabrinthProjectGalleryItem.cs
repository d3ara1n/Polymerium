namespace Polymerium.Trident.Models.Labrinth;

public struct LabrinthProjectGalleryItem
{
    public Uri Url { get; set; }
    public bool Featured { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTimeOffset Created { get; set; }
    public int Ordering { get; set; }
}