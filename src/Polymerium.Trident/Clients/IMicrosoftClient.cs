using Polymerium.Trident.Models.Microsoft;
using Polymerium.Trident.Services;
using Refit;

namespace Polymerium.Trident.Clients;

public interface IMicrosoftClient
{
    [Post("/consumers/oauth2/v2.0/devicecode")]
    Task<DeviceCodeResponse> AcquireUserCodeAsync(
        string clientId = MicrosoftService.CLIENT_ID,
        string scope = MicrosoftService.SCOPE);

    [Post("/consumers/oauth2/v2.0/token")]
    Task<TokenResponse> AuthenticateAsync(
        string deviceCode,
        string grantType = "urn:ietf:params:oauth:grant-type:device_code",
        string clientId = MicrosoftService.CLIENT_ID);

    [Post("/consumers/oauth2/v2.0/token")]
    Task<TokenResponse> RefreshUserAsync(
        string refreshToken,
        string grantType = "refresh_token",
        string clientId = MicrosoftService.CLIENT_ID);
}