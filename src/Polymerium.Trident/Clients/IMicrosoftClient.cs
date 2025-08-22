using Polymerium.Trident.Models.MicrosoftApi;
using Refit;

namespace Polymerium.Trident.Clients;

public interface IMicrosoftClient
{
    [Post("/consumers/oauth2/v2.0/devicecode")]
    Task<DeviceCodeResponse> AcquireUserCodeAsync(
        [Body(BodySerializationMethod.UrlEncoded)]
        AcquireUserCodeRequest request);

    [Post("/consumers/oauth2/v2.0/token")]
    Task<TokenResponse> AuthenticateAsync([Body(BodySerializationMethod.UrlEncoded)] AuthenticateRequest request);

    [Post("/consumers/oauth2/v2.0/token")]
    Task<TokenResponse> RefreshUserAsync([Body(BodySerializationMethod.UrlEncoded)] RefreshUserRequest request);
}