namespace Polymerium.Trident.Models.Eternal;

public struct EternalModCategory
{
    public uint Id { get; init; }
    public uint GameId { get; init; }
    public string Name { get; init; }
    public string Slug { get; init; }
    public Uri Url { get; init; }
    public Uri IconUrl { get; init; }
    public DateTime DateModified { get; init; }
    public bool IsClass { get; init; }
    public uint ClassId { get; init; }
    public uint ParentCategoryId { get; init; }
    public int DisplayIndex { get; init; }
}