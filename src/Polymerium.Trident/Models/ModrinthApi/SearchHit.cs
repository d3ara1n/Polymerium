namespace Polymerium.Trident.Models.ModrinthApi;

public record SearchHit(
    string ProjectId,
    string ProjectType,
    string Slug,
    string Author,
    string Title,
    string Description,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> DisplayCategories,
    IReadOnlyList<string> Versions,
    uint Downloads,
    uint Follows,
    Uri IconUrl,
    DateTimeOffset DateCreated,
    DateTimeOffset DateModified,
    string LatestVersion,
    string License,
    string ClientSide,
    string ServerSide,
    IReadOnlyList<Uri> Gallery,
    Uri FeaturedGallery,
    long? Color);