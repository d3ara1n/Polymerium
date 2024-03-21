namespace Polymerium.Trident.Models.Minecraft
{
    public struct MinecraftLoginResponse
    {
        public string Username { get; set; }
        public IEnumerable<object> Roles { get; set; }
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
    }
}