using Polymerium.Trident.Models.XboxLive;
using System.Net.Http.Json;
using System.Text.Json;

namespace Polymerium.Trident.Helpers
{
    public static class XboxLiveHelper
    {
        private const string XBOX_ENDPOINT = "https://user.auth.xboxlive.com/user/authenticate";
        private const string XSTS_ENDPOINT = "https://xsts.auth.xboxlive.com/xsts/authorize";

        private static readonly JsonSerializerOptions OPTIONS = new(JsonSerializerDefaults.General)
        {
            PropertyNameCaseInsensitive = true
        };

        public static async Task<XboxLiveResponse> AuthenticateForXboxLiveTokenByMicrosoftTokenAsync(
            IHttpClientFactory factory, string accessToken)
        {
            using var client = factory.CreateClient();
            var parameter = new
            {
                Properties = new
                {
                    AuthMethod = "RPS",
                    SiteName = "user.auth.xboxlive.com",
                    RpsTicket = $"d={accessToken}"
                },
                RelyingParty = "http://auth.xboxlive.com",
                TokenType = "JWT"
            };
            var response = await client.PostAsJsonAsync(XBOX_ENDPOINT, parameter, OPTIONS);
            response.EnsureSuccessStatusCode();
            var model = await response.Content.ReadFromJsonAsync<XboxLiveResponse>(OPTIONS);
            return model;
        }

        public static async Task<XboxLiveResponse> AuthorizeForServiceTokenByXboxLiveTokenAsync(
            IHttpClientFactory factory, string token)
        {
            using var client = factory.CreateClient();
            var parameter = new
            {
                Properties = new { SandboxId = "RETAIL", UserTokens = new[] { token } },
                RelyingParty = "rp://api.minecraftservices.com/",
                TokenType = "JWT"
            };
            var response = await client.PostAsJsonAsync(XSTS_ENDPOINT, parameter, OPTIONS);
            response.EnsureSuccessStatusCode();
            var model = await response.Content.ReadFromJsonAsync<XboxLiveResponse>(OPTIONS);
            return model;
        }
    }
}