namespace Polymerium.Trident.Models.Minecraft;

public struct MinecraftProfileResponse
{
    public string Id { get; set; }
    public string Name { get; set; }
    public IEnumerable<MinecraftProfileSkin> Skins { get; set; }
    public IEnumerable<object> Capes { get; set; }
    public string Error { get; set; }
    public string ErrorMessage { get; set; }

    public struct MinecraftProfileSkin
    {
        public string Id { get; set; }
        public string State { get; set; }
        public Uri Url { get; set; }
        public string Variant { get; set; }
        public string Alias { get; set; }
    }
}