using Polymerium.Trident.Clients;
using Polymerium.Trident.Exceptions;
using Polymerium.Trident.Models.MicrosoftApi;

namespace Polymerium.Trident.Services;

public class MicrosoftService(IMicrosoftClient client)
{
    public const string ENDPOINT = "https://login.microsoftonline.com";
    public const string CLIENT_ID = "66b049dc-22a1-4fd8-a17d-2ccd01332101";
    public const string SCOPE = "XboxLive.signin offline_access";

    public async Task<DeviceCodeResponse> AcquireUserCodeAsync() =>
        await client.AcquireUserCodeAsync(new()).ConfigureAwait(false);

    public async Task<TokenResponse> AuthenticateAsync(
        string deviceCode,
        int interval,
        CancellationToken token = default)
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();
            var response = await client.AuthenticateAsync(new(deviceCode)).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(response.Error))
            {
                if (response.Error == "authorization_pending")
                {
                    await Task.Delay(TimeSpan.FromSeconds(interval), token).ConfigureAwait(false);
                    continue;
                }

                throw new AccountAuthenticationException(response.ErrorDescription ?? response.Error);
            }

            return response;
        }
    }

    public async Task<TokenResponse> RefreshUserAsync(string refreshToken) =>
        await client.RefreshUserAsync(new(refreshToken)).ConfigureAwait(false);
}