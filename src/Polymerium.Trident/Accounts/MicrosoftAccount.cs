using Polymerium.Trident.Exceptions;
using Polymerium.Trident.Helpers;
using Trident.Abstractions;

namespace Polymerium.Trident.Accounts;

public class MicrosoftAccount(string accessToken, string refreshToken, string uuid, string username) : IAccount
{
    public string RefreshToken { get; private set; } = refreshToken;
    public string Username { get; private set; } = username;

    public string Uuid { get; private set; } = uuid;

    public string AccessToken { get; private set; } = accessToken;

    public string UserType => "msa";

    public async ValueTask<bool> RefreshAsync(IHttpClientFactory factory, CancellationToken token)
    {
        if (token.IsCancellationRequested) return false;
        try
        {
            var microsoft =
                await MicrosoftHelper.RefreshUserAsync(factory, RefreshToken, 5, token);

            AccessToken = microsoft.AccessToken;
            RefreshToken = microsoft.RefreshToken;

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

            Uuid = profile.Id;
            Username = profile.Name;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async ValueTask<bool> ValidateAsync(IHttpClientFactory factory, CancellationToken token)
    {
        if (token.IsCancellationRequested) return false;
        try
        {
            var profile = await MinecraftHelper.AcquireAccountProfileByMinecraftTokenAsync(factory, AccessToken);
            return string.IsNullOrEmpty(profile.Error);
        }
        catch
        {
            return false;
        }
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