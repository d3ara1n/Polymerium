using Polymerium.Trident.Models.MinecraftApi;
using Refit;

namespace Polymerium.Trident.Clients
{
    public interface IMinecraftClient
    {
        [Post("/authentication/login_with_xbox")]
        Task<MinecraftLoginResponse> AcquireAccessTokenByXboxServiceTokenAsync(
            [Body] AcquireAccessTokenByXboxServiceTokenRequest request);

        [Get("/entitlements/mcstore")]
        Task<MinecraftStoreResponse> AcquireAccountInventoryByAccessTokenAsync([Authorize] string accessToken);

        [Get("/minecraft/profile")]
        Task<MinecraftProfileResponse> AcquireAccountProfileByMinecraftTokenAsync([Authorize] string accessToken);
    }
}
