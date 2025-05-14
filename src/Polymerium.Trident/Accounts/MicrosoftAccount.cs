using Trident.Abstractions.Accounts;

namespace Polymerium.Trident.Accounts;

public class MicrosoftAccount : IAccount
{
    public required string RefreshToken { get; set; }
    public required string Username { get; init; }

    public required string Uuid { get; init; }
    public required string AccessToken { get; set; }

    public string UserType => "msa";

    public async ValueTask<bool> RefreshAsync(CancellationToken token) =>
        // if (token.IsCancellationRequested) return false;
        // try
        // {
        //     var microsoft =
        //         await MicrosoftHelper.RefreshUserAsync(factory, RefreshToken, 5, token);
        //
        //     AccessToken = microsoft.AccessToken;
        //     RefreshToken = microsoft.RefreshToken;
        //
        //     var xbox =
        //         await XboxLiveHelper.AuthenticateForXboxLiveTokenByMicrosoftTokenAsync(factory, microsoft.AccessToken);
        //     var xsts =
        //         await XboxLiveHelper.AuthorizeForServiceTokenByXboxLiveTokenAsync(factory, xbox.Token);
        //     var minecraft =
        //         await MinecraftHelper.AuthenticateByXboxLiveServiceTokenAsync(factory, xsts.Token,
        //             xsts.DisplayClaims.Xui.First().Uhs);
        //     var inventory =
        //         await MinecraftHelper.AcquireAccountInventoryByMinecraftTokenAsync(factory, minecraft.AccessToken);
        //     if (!inventory.Items.Any())
        //     {
        //         throw new AccountAuthenticationException("The account does not own the game");
        //     }
        //
        //     var profile =
        //         await MinecraftHelper.AcquireAccountProfileByMinecraftTokenAsync(factory, minecraft.AccessToken);
        //
        //     Uuid = profile.Id;
        //     Username = profile.Name;
        //     return true;
        // }
        // catch
        // {
        //     return false;
        // }
        false;

    public async ValueTask<bool> ValidateAsync(CancellationToken token) =>
        // if (token.IsCancellationRequested) return false;
        // try
        // {
        //     var profile = await MinecraftHelper.AcquireAccountProfileByMinecraftTokenAsync(factory, AccessToken);
        //     return string.IsNullOrEmpty(profile.Error);
        // }
        // catch
        // {
        //     return false;
        // }
        false;
}