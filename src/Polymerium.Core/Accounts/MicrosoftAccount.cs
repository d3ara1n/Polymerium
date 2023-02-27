using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Accounts;
using Polymerium.Core.Helpers;

namespace Polymerium.Core.Accounts;

public class MicrosoftAccount : IGameAccount
{
    public MicrosoftAccount(string id, string uuid, string nickname, string accessToken, string refreshToken)
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
    public string LoginType => "mojang";

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
            AccessToken = account.AccessToken;
            RefreshToken = account.RefreshToken;
        }

        return accountOption.IsSome();
    }

    public static async Task<Result<MicrosoftAccount, string>> LoginAsync(Action<string, string> userCodeCallback,
        CancellationToken token = default)
    {
        var deviceCodeOption =
            await MicrosoftAccountHelper.AcquireMicrosoftTokenByDeviceCodeAsync(userCodeCallback, token);
        if (token.IsCancellationRequested) return Result<MicrosoftAccount, string>.Err("操作取消");
        if (deviceCodeOption.TryUnwrap(out var flow))
        {
            var refreshToken = flow.RefreshToken;
            var microsoftToken = flow.AccessToken;
            var option = await LoginAsync(microsoftToken, token);
            if (option.IsOk(out var account)) account!.RefreshToken = refreshToken;
            return option;
        }

        return Result<MicrosoftAccount, string>.Err("通过设备码获取微软账号时出现网络异常或超时");
    }

    public static async Task<Result<MicrosoftAccount, string>> LoginAsync(string microsoftAccessToken,
        CancellationToken token)
    {
        // 这！就是嵌套地狱！
        var account = new MicrosoftAccount { Id = Guid.NewGuid().ToString() };
        var xboxOption = await MicrosoftAccountHelper.AcquireXboxTokenAsync(microsoftAccessToken, token);
        if (token.IsCancellationRequested) return Result<MicrosoftAccount, string>.Err("操作取消");
        if (xboxOption.TryUnwrap(out var xbox))
        {
            var xstsOption = await MicrosoftAccountHelper.AcquireXstsTokenAsync(xbox.Token, token);
            if (token.IsCancellationRequested) return Result<MicrosoftAccount, string>.Err("操作取消");
            if (xstsOption.TryUnwrap(out var xsts))
            {
                var minecraftOption =
                    await MicrosoftAccountHelper.AcquireMinecraftTokenAsync(xsts.Token,
                        xsts.DisplayClaims.Xui.First().Uhs, token);
                if (token.IsCancellationRequested) return Result<MicrosoftAccount, string>.Err("操作取消");
                if (minecraftOption.TryUnwrap(out var minecraft))
                {
                    account.AccessToken = minecraft.AccessToken;
                    var ownership =
                        await MicrosoftAccountHelper.VerifyMinecraftOwnershipAsync(minecraft.AccessToken, token);
                    if (token.IsCancellationRequested) return Result<MicrosoftAccount, string>.Err("操作取消");
                    if (ownership)
                    {
                        var profileOption =
                            await MicrosoftAccountHelper.GetProfileByAccessTokenAsync(minecraft.AccessToken, token);
                        if (token.IsCancellationRequested) return Result<MicrosoftAccount, string>.Err("操作取消");
                        if (profileOption.TryUnwrap(out var profile))
                        {
                            account.UUID = profile.Id;
                            account.Nickname = profile.Name;
                            return Result<MicrosoftAccount, string>.Ok(account);
                        }

                        return Result<MicrosoftAccount, string>.Err("获取玩家信息失败");
                    }

                    return Result<MicrosoftAccount, string>.Err("无法验证用户的游戏所有权");
                }

                return Result<MicrosoftAccount, string>.Err("Minecraft 获取授权时出现网络异常或未授权");
            }

            return Result<MicrosoftAccount, string>.Err("Xbox Live XSTS 获取授权时出现网络异常或未授权");
        }

        return Result<MicrosoftAccount, string>.Err("Xbox Live 获取授权时出现网络异常或未授权");
    }
}