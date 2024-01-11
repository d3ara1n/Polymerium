namespace Polymerium.Trident.Models.Eternal;

public struct EternalProjectCategory
{
    public uint Id { get; set; }
    public uint GameId { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    public Uri Url { get; set; }
    public Uri IconUrl { get; set; }
    public DateTime DateModified { get; set; }
    public bool IsClass { get; set; }
    public uint ClassId { get; set; }
    public uint ParentCategoryId { get; set; }
    public int DisplayIndex { get; set; }
}