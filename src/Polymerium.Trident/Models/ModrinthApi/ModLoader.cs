namespace Polymerium.Trident.Models.ModrinthApi;

public record ModLoader(
    string Icon,
    string Name,
    IReadOnlyList<string> SupportedProjectTypes,
    IReadOnlyList<string> SupportedGames,
    IReadOnlyList<string> SupportedFields,
    ModLoader.LoaderMetadata Metadata)
{
    public record LoaderMetadata(bool? Platform);
}