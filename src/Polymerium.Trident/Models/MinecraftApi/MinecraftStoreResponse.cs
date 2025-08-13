namespace Polymerium.Trident.Models.MinecraftApi
{
    public readonly record struct MinecraftStoreResponse(
        string? Error,
        string? ErrorMessage,
        IReadOnlyList<MinecraftStoreResponse.Item> Items,
        string Signature,
        int KeyId)
    {
        #region Nested type: Item

        public readonly record struct Item(string Name, string Signature);

        #endregion
    }
}
