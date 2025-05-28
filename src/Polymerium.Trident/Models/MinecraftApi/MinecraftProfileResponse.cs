namespace Polymerium.Trident.Models.MinecraftApi;

public record MinecraftProfileResponse(
    string? Error,
    string? ErrorMessage,
    string Id,
    string Name,
    MinecraftProfileResponse.Skin[] Skins,
    MinecraftProfileResponse.Cape[] Capes)
{
    #region Nested type: Cape

    public record Cape(string Id, string State, Uri Url, string Alias);

    #endregion

    #region Nested type: Skin

    public record Skin(string Id, string State, Uri Url, string Variant, string Alias);

    #endregion
}