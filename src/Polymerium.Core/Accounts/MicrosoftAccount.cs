using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNext;
using Polymerium.Abstractions.Accounts;
using Polymerium.Core.Helpers;

namespace Polymerium.Core.Accounts;

public class MicrosoftAccount : IGameAccount
{
    public enum MicrosoftAccountError
    {
        Canceled,
        ProfileFailed,
        OwnershipFailed,
        MinecraftAuthenticationFailed,
        XboxAuthenticationFailed,
        XstsAuthenticationFailed,
        MicrosoftAuthenticationFailed
    }

    public MicrosoftAccount(
        string id,
        string uuid,
        string nickname,
        string accessToken,
        string refreshToken
    )
    {
        Id = id;
        UUID = uuid;
        Nickname = nickname;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
    }

    private MicrosoftAccount()
    {
        Id = string.Empty;
        UUID = string.Empty;
        Nickname = string.Empty;
        AccessToken = string.Empty;
        RefreshToken = string.Empty;
    }

    public string RefreshToken { get; set; }
    public string LoginType => "msa";
    public string FriendlyType => "微软账号";

    public string Id { get; set; }
    public string UUID { get; set; }
    public string Nickname { get; set; }
    public string AccessToken { get; set; }

    public async Task<bool> ValidateAsync()
    {
        return (await MicrosoftAccountHelper.GetProfileByAccessTokenAsync(AccessToken)).IsSome();
    }

    public async Task<bool> RefreshAsync()
    {
        var accountOption = await MicrosoftAccountHelper.RefreshAccessTokenAsync(RefreshToken);
        if (accountOption.TryUnwrap(out var account))
        {
            RefreshToken = account.RefreshToken;
            var result = await LoginAsync(account.AccessToken, CancellationToken.None);
            if (result.IsSuccessful)
            {
                AccessToken = result.Value.AccessToken;
                UUID = result.Value.UUID;
                Nickname = result.Value.Nickname;
                return true;
            }
        }

        return false;
    }

    public static async Task<Result<MicrosoftAccount, MicrosoftAccountError>> LoginAsync(
        Action<string, string> userCodeCallback,
        CancellationToken token = default
    )
    {
        var deviceCodeOption = await MicrosoftAccountHelper.AcquireMicrosoftTokenByDeviceCodeAsync(
            userCodeCallback,
            token
        );
        if (token.IsCancellationRequested)
            return new Result<MicrosoftAccount, MicrosoftAccountError>(
                MicrosoftAccountError.Canceled
            );
        if (deviceCodeOption.TryUnwrap(out var flow))
        {
            var refreshToken = flow.RefreshToken;
            var microsoftToken = flow.AccessToken;
            var result = await LoginAsync(microsoftToken, token);
            if (result.IsSuccessful)
                result.Value.RefreshToken = refreshToken;
            return result;
        }

        return new Result<MicrosoftAccount, MicrosoftAccountError>(
            MicrosoftAccountError.MicrosoftAuthenticationFailed
        );
    }

    public static async Task<Result<MicrosoftAccount, MicrosoftAccountError>> LoginAsync(
        string microsoftAccessToken,
        CancellationToken token
    )
    {
        // 这！就是嵌套地狱！
        var account = new MicrosoftAccount { Id = Guid.NewGuid().ToString() };
        var xboxOption = await MicrosoftAccountHelper.AcquireXboxTokenAsync(
            microsoftAccessToken,
            token
        );
        if (token.IsCancellationRequested)
            return new Result<MicrosoftAccount, MicrosoftAccountError>(
                MicrosoftAccountError.Canceled
            );
        if (xboxOption.TryUnwrap(out var xbox))
        {
            var xstsOption = await MicrosoftAccountHelper.AcquireXstsTokenAsync(xbox.Token, token);
            if (token.IsCancellationRequested)
                return new Result<MicrosoftAccount, MicrosoftAccountError>(
                    MicrosoftAccountError.Canceled
                );
            if (xstsOption.TryUnwrap(out var xsts))
            {
                var minecraftOption = await MicrosoftAccountHelper.AcquireMinecraftTokenAsync(
                    xsts.Token,
                    xsts.DisplayClaims.Xui.First().Uhs,
                    token
                );
                if (token.IsCancellationRequested)
                    return new Result<MicrosoftAccount, MicrosoftAccountError>(
                        MicrosoftAccountError.Canceled
                    );
                if (minecraftOption.TryUnwrap(out var minecraft))
                {
                    account.AccessToken = minecraft.AccessToken;
                    var ownership = await MicrosoftAccountHelper.VerifyMinecraftOwnershipAsync(
                        minecraft.AccessToken,
                        token
                    );
                    if (token.IsCancellationRequested)
                        return new Result<MicrosoftAccount, MicrosoftAccountError>(
                            MicrosoftAccountError.Canceled
                        );
                    if (ownership)
                    {
                        var profileOption =
                            await MicrosoftAccountHelper.GetProfileByAccessTokenAsync(
                                minecraft.AccessToken,
                                token
                            );
                        if (token.IsCancellationRequested)
                            return new Result<MicrosoftAccount, MicrosoftAccountError>(
                                MicrosoftAccountError.Canceled
                            );
                        if (profileOption.TryUnwrap(out var profile))
                        {
                            account.UUID = profile.Id;
                            account.Nickname = profile.Name;
                            return account;
                        }

                        return new Result<MicrosoftAccount, MicrosoftAccountError>(
                            MicrosoftAccountError.ProfileFailed
                        );
                    }

                    return new Result<MicrosoftAccount, MicrosoftAccountError>(
                        MicrosoftAccountError.OwnershipFailed
                    );
                }

                return new Result<MicrosoftAccount, MicrosoftAccountError>(
                    MicrosoftAccountError.MinecraftAuthenticationFailed
                );
            }

            return new Result<MicrosoftAccount, MicrosoftAccountError>(
                MicrosoftAccountError.XstsAuthenticationFailed
            );
        }

        return new Result<MicrosoftAccount, MicrosoftAccountError>(
            MicrosoftAccountError.XboxAuthenticationFailed
        );
    }
}
