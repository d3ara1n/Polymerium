using System.Text.Json.Serialization;

namespace Polymerium.Trident.Models.MinecraftApi
{
    public readonly record struct AcquireAccessTokenByXboxServiceTokenRequest(
        [property: JsonPropertyName("identityToken")]
        string IdentityToken);
}
