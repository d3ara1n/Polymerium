using CmlLib.Core.Auth.Microsoft.MsalClient;
using CmlLib.Core.Auth.Microsoft;
using Newtonsoft.Json.Linq;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Accounts;
using System;
using System.Threading;
using System.Threading.Tasks;
using Wupoo;
using System.Net.Http;
using System.Collections.Generic;

namespace Polymerium.Core.Accounts;

public class MicrosoftAccount : IGameAccount
{
    private const string DEVICE_ENDPOINT = "https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode";
    private const string TOKEN_ENDPOINT = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
    private const string PROFILE_ENDPOINT = "https://api.minecraftservices.com/minecraft/profile";
    private const string CLIENT_ID = "66b049dc-22a1-4fd8-a17d-2ccd01332101";
    private const string SCOPE = "XboxLive.signin offline_access";

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

    public string Id { get; set; }
    public string UUID { get; set; }
    public string Nickname { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }

    public static async Task<Result<MicrosoftAccount, Exception>> LoginAsync(Action<string, string> userCodeCallback, CancellationToken token = default)
    {
        var app = MsalMinecraftLoginHelper.CreateDefaultApplicationBuilder(CLIENT_ID)
            .Build();
        var handler = new LoginHandlerBuilder().ForJavaEdition()
            .WithMsalOAuth(app, factory => factory.CreateDeviceCodeApi(code =>
            {
                userCodeCallback(code.UserCode, code.VerificationUrl);
                return Task.CompletedTask;
            }))
            .Build();
        try
        {
            var session = await handler.LoginFromOAuth(token);
            var account = new MicrosoftAccount(Guid.NewGuid().ToString(), session.GameSession.UUID!,
                session.GameSession.Username!, session.GameSession.AccessToken!, session.MicrosoftOAuthToken?.RawRefreshToken);
            return Result<MicrosoftAccount, Exception>.Ok(account);
        }
        catch (Exception e)
        {
            return Result<MicrosoftAccount, Exception>.Err(e);
        }
    }

    public async Task<bool> ValidateAsync()
    {
        var succ = false;
        await Wapoo.Wohoo(PROFILE_ENDPOINT)
            .ViaGet()
            .UseBearer(AccessToken)
            .WhenCode(204, _ => succ = true)
            .FetchAsync();
        return succ;
    }

    public async Task<bool> RefreshAsync()
    {
        // TODO: 得自己写 login 流程

        return true;

        var succ = false;
        await Wapoo.Wohoo(TOKEN_ENDPOINT)
            .ViaPost()
            .WithBody(new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                {"client_id",CLIENT_ID },
                {"refresh_token",  RefreshToken},
                {"grant_type","refresh_token" }
            }))
            .ForJsonResult<JObject>(x =>
            {
                string? accessToken = null;
                string? refreshToken = null;
                if (x.ContainsKey("access_token"))
                {
                    accessToken = x["access_token"]!.Value<string>();
                }
                if (x.ContainsKey("refresh_token"))
                {
                    refreshToken = x["refresh_token"]!.Value<string>();
                }
                if (accessToken != null && refreshToken != null)
                {
                    AccessToken = accessToken!;
                    RefreshToken = refreshToken!;
                    succ = true;
                }
            })
            .FetchAsync();
        return succ;
    }
}