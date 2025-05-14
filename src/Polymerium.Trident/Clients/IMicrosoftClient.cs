using Polymerium.Trident.Models.MicrosoftApi;
using Refit;

namespace Polymerium.Trident.Clients;

public interface IMicrosoftClient
{
    [Post("/consumers/oauth2/v2.0/devicecode")]
    Task<DeviceCodeResponse> AcquireUserCodeAsync([Body] AcquireUserCodeRequest request);

    [Post("/consumers/oauth2/v2.0/token")]
    Task<TokenResponse> AuthenticateAsync([Body] AuthenticateRequest request);

    [Post("/consumers/oauth2/v2.0/token")]
    Task<TokenResponse> RefreshUserAsync([Body] RefreshUserRequest request);
}