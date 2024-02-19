using System.Text.Json.Serialization;

namespace Polymerium.Trident.Models.Minecraft
{
    public struct MinecraftVersionDownloads
    {
        public MinecraftVersionDownload Client { get; init; }
        [JsonPropertyName("client_mappings")] public MinecraftVersionDownload ClientMappings { get; init; }
        public MinecraftVersionDownload Server { get; init; }
        [JsonPropertyName("server_mappings")] public MinecraftVersionDownload ServerMappings { get; init; }
    }
}