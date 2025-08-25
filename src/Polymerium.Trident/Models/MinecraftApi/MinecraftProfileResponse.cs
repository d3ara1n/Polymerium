namespace Polymerium.Trident.Models.MinecraftApi
{
    public readonly record struct MinecraftProfileResponse(
        string? Error,
        string? ErrorMessage,
        string Id,
        string Name,
        IReadOnlyList<MinecraftProfileResponse.Skin> Skins,
        IReadOnlyList<MinecraftProfileResponse.Cape> Capes)
    {
        #region Nested type: Cape

        public readonly record struct Cape(string Id, string State, Uri Url, string Alias);

        #endregion

        #region Nested type: Skin

        public readonly record struct Skin(string Id, string State, Uri Url, string Variant, string Alias);

        #endregion
    }
}
