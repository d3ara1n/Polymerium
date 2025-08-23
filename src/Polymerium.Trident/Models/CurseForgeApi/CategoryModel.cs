namespace Polymerium.Trident.Models.CurseForgeApi
{
    public readonly record struct CategoryModel(
        uint Id,
        uint GameId,
        string Name,
        string Slug,
        Uri Url,
        Uri IconUrl,
        DateTimeOffset DateModified,
        bool? IsClass,
        uint? ClassId,
        uint? ParentCategoryId,
        uint? DisplayIndex);
}
