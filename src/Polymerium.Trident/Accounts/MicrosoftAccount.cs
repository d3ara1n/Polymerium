using Polymerium.Trident.Exceptions;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Models.Microsoft;
using Polymerium.Trident.Models.Minecraft;
using Polymerium.Trident.Models.XboxLive;
using Trident.Abstractions;

namespace Polymerium.Trident.Accounts
{
    public class MicrosoftAccount(string accessToken, string refreshToken, string uuid, string username) : IAccount
    {
        public string RefreshToken { get; } = refreshToken;
        public string Username { get; } = username;

        public string Uuid { get; } = uuid;

        public string AccessToken { get; } = accessToken;

        public string UserType => "msa";

        public ValueTask<bool> RefreshAsync()
        {
            // TODO: MUST FIX
            return ValueTask.FromResult(true);
        }

        public ValueTask<bool> ValidateAsync()
        {
            return ValueTask.FromResult(true);
        }

        public static async Task<MicrosoftAccount> LoginAsync(IHttpClientFactory factory,
            Action<string, Uri?> codeCallback, CancellationToken token = default)
        {
            var deviceCode = await MicrosoftHelper.AcquireUserCodeAsync(factory);
            codeCallback(deviceCode.UserCode, deviceCode.VerificationUri);
            var microsoft =
                await MicrosoftHelper.AuthenticateUserAsync(factory, deviceCode.DeviceCode, deviceCode.Interval, token);
            var xbox =
                await XboxLiveHelper.AuthenticateForXboxLiveTokenByMicrosoftTokenAsync(factory, microsoft.AccessToken);
            var xsts =
                await XboxLiveHelper.AuthorizeForServiceTokenByXboxLiveTokenAsync(factory, xbox.Token);
            var minecraft =
                await MinecraftHelper.AuthenticateByXboxLiveServiceTokenAsync(factory, xsts.Token,
                    xsts.DisplayClaims.Xui.First().Uhs);
            var inventory =
                await MinecraftHelper.AcquireAccountInventoryByMinecraftTokenAsync(factory, minecraft.AccessToken);
            if (!inventory.Items.Any())
            {
                throw new AccountAuthenticationException("The account does not own the game");
            }

            var profile =
                await MinecraftHelper.AcquireAccountProfileByMinecraftTokenAsync(factory, minecraft.AccessToken);
            return new MicrosoftAccount(minecraft.AccessToken, microsoft.RefreshToken, profile.Id, profile.Name);
        }
    }
}