namespace Polymerium.Trident.Models.Minecraft;

public struct MinecraftStoreResponse
{
    public IEnumerable<MinecraftStoreItem> Items { get; set; }
    public string Signature { get; set; }
    public string KeyId { get; set; }

    public struct MinecraftStoreItem
    {
        public string Name { get; set; }
        public string Signature { get; set; }
    }
}