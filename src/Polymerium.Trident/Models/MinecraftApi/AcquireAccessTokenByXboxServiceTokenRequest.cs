using System.Text.Json.Serialization;

namespace Polymerium.Trident.Models.MinecraftApi;

public record AcquireAccessTokenByXboxServiceTokenRequest(
    [property: JsonPropertyName("identityToken")]
    string IdentityToken);