using Polymerium.Trident.Exceptions;
using Polymerium.Trident.Models.Minecraft;
using System.Net.Http.Json;
using System.Text.Json;

namespace Polymerium.Trident.Helpers
{
    public static class MinecraftHelper
    {
        private const string LOGIN_ENDPOINT = "https://api.minecraftservices.com/authentication/login_with_xbox";
        private const string STORE_ENDPOINT = "https://api.minecraftservices.com/entitlements/mcstore";
        private const string PROFILE_ENDPOINT = "https://api.minecraftservices.com/minecraft/profile";

        private static readonly JsonSerializerOptions REQUEST_OPTIONS = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static readonly JsonSerializerOptions RESPONSE_OPTIONS = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public static async Task<MinecraftLoginResponse> AuthenticateByXboxLiveServiceTokenAsync(
            IHttpClientFactory factory, string token, string hash)
        {
            using HttpClient client = factory.CreateClient();
            var parameter = new { IdentityToken = $"XBL3.0 x={hash};{token}" };
            HttpResponseMessage response = await client.PostAsJsonAsync(LOGIN_ENDPOINT, parameter, REQUEST_OPTIONS);
            response.EnsureSuccessStatusCode();
            MinecraftLoginResponse model =
                await response.Content.ReadFromJsonAsync<MinecraftLoginResponse>(RESPONSE_OPTIONS);
            return model;
        }

        public static async Task<MinecraftStoreResponse> AcquireAccountInventoryByMinecraftTokenAsync(
            IHttpClientFactory factory, string token)
        {
            using HttpClient client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            MinecraftStoreResponse model =
                await client.GetFromJsonAsync<MinecraftStoreResponse>(STORE_ENDPOINT, RESPONSE_OPTIONS);
            return model;
        }

        public static async Task<MinecraftProfileResponse> AcquireAccountProfileByMinecraftTokenAsync(
            IHttpClientFactory factory, string token)
        {
            using HttpClient client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            MinecraftProfileResponse model =
                await client.GetFromJsonAsync<MinecraftProfileResponse>(PROFILE_ENDPOINT, RESPONSE_OPTIONS);
            if (!string.IsNullOrEmpty(model.Error))
            {
                throw new AccountAuthenticationException(model.ErrorMessage);
            }

            return model;
        }
    }
}