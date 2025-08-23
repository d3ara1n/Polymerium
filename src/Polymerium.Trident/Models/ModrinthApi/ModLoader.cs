namespace Polymerium.Trident.Models.ModrinthApi
{
    public readonly record struct ModLoader(
        string Icon,
        string Name,
        IReadOnlyList<string> SupportedProjectTypes,
        IReadOnlyList<string> SupportedGames,
        IReadOnlyList<string> SupportedFields,
        ModLoader.LoaderMetadata Metadata)
    {
        #region Nested type: LoaderMetadata

        public readonly record struct LoaderMetadata(bool? Platform);

        #endregion
    }
}
