using Polymerium.Trident.Clients;
using Polymerium.Trident.Exceptions;
using Polymerium.Trident.Models.MinecraftApi;

namespace Polymerium.Trident.Services
{
    public class MinecraftService(IMinecraftClient client)
    {
        public const string ENDPOINT = "https://api.minecraftservices.com";

        public async Task<MinecraftLoginResponse> AuthenticateByXboxLiveServiceTokenAsync(string token, string uhs)
        {
            var response = await client
                                .AcquireAccessTokenByXboxServiceTokenAsync(new($"XBL3.0 x={uhs};{token}"))
                                .ConfigureAwait(false);
            if (!string.IsNullOrEmpty(response.Error))
            {
                throw response.Error switch
                {
                    "Forbidden" => new MinecraftGameNotOwnedException(response.ErrorMessage ?? "No message provided"),
                    _ => new AccountAuthenticationException(response.ErrorMessage ?? "No message provided")
                };
            }

            return response;
        }

        public async Task<MinecraftProfileResponse> AcquireAccountProfileByMinecraftTokenAsync(string accessToken)
        {
            var response = await client.AcquireAccountProfileByMinecraftTokenAsync(accessToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(response.Error))
            {
                throw new AccountAuthenticationException(response.ErrorMessage ?? "No message provided");
            }

            return response;
        }
    }
}
