namespace Polymerium.Trident.Models.ModrinthApi;

public record ModLoader(
    string Icon,
    string Name,
    IReadOnlyList<string> SupportedProjectTypes,
    IReadOnlyList<string> SupportedGames,
    IReadOnlyList<string> SupportedFields,
    ModLoader.LoaderMetadata Metadata)
{
    #region Nested type: LoaderMetadata

    public record LoaderMetadata(bool? Platform);

    #endregion
}