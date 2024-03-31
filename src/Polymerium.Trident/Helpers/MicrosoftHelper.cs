using Polymerium.Trident.Exceptions;
using Polymerium.Trident.Models.Microsoft;
using System.Net.Http.Json;
using System.Text.Json;

namespace Polymerium.Trident.Helpers
{
    public static class MicrosoftHelper
    {
        private const string DEVICE_ENDPOINT = "https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode";
        private const string TOKEN_ENDPOINT = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
        private const string CLIENT_ID = "66b049dc-22a1-4fd8-a17d-2ccd01332101";
        private const string SCOPE = "XboxLive.signin offline_access";

        private static readonly JsonSerializerOptions OPTIONS = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public static async Task<MicrosoftDeviceCodeResponse> AcquireUserCodeAsync(IHttpClientFactory factory)
        {
            using var client = factory.CreateClient();
            var response = await client.PostAsync(DEVICE_ENDPOINT,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "client_id", CLIENT_ID }, { "scope", SCOPE }
                }));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MicrosoftDeviceCodeResponse>(OPTIONS);
        }

        public static async Task<MicrosoftAuthenticationResponse> AuthenticateUserAsync(IHttpClientFactory factory,
            string deviceCode, int interval = 5, CancellationToken token = default)
        {
            using var client = factory.CreateClient();
            while (true)
            {
                token.ThrowIfCancellationRequested();
                var response = await client.PostAsync(TOKEN_ENDPOINT,
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "grant_type", "urn:ietf:params:oauth:grant-type:device_code" },
                        { "client_id", CLIENT_ID },
                        { "device_code", deviceCode }
                    }));
                var whoa =
                    await response.Content.ReadFromJsonAsync<MicrosoftAuthenticationResponse>(OPTIONS);
                if (!string.IsNullOrEmpty(whoa.Error))
                {
                    if (whoa.Error == "authorization_pending")
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(interval), token);
                        continue;
                    }

                    throw new AccountAuthenticationException(whoa.Error);
                }

                return whoa;
            }
        }
    }
}