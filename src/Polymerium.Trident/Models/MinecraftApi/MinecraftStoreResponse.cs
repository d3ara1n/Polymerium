namespace Polymerium.Trident.Models.MinecraftApi;

public record MinecraftStoreResponse(
    string? Error,
    string? ErrorMessage,
    MinecraftStoreResponse.Item[] Items,
    string Signature,
    int KeyId)
{
    #region Nested type: Item

    public record Item(string Name, string Signature);

    #endregion
}