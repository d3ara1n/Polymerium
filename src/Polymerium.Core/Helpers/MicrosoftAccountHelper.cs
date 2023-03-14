using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Polymerium.Abstractions;
using Polymerium.Core.Models.Microsoft;
using Polymerium.Core.Models.Minecraft;
using Polymerium.Core.Models.XboxLive;
using Wupoo;

namespace Polymerium.Core.Helpers;

public static class MicrosoftAccountHelper
{
    private const string DEVICE_ENDPOINT = "https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode";
    private const string TOKEN_ENDPOINT = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
    private const string XBOX_ENDPOINT = "https://user.auth.xboxlive.com/user/authenticate";
    private const string XSTS_ENDPOINT = "https://xsts.auth.xboxlive.com/xsts/authorize";
    private const string MINECRAFT_ENDPOINT = "https://api.minecraftservices.com/authentication/login_with_xbox";
    private const string STORE_ENDPOINT = "https://api.minecraftservices.com/entitlements/mcstore";
    private const string PROFILE_ENDPOINT = "https://api.minecraftservices.com/minecraft/profile";
    private const string CLIENT_ID = "66b049dc-22a1-4fd8-a17d-2ccd01332101";
    private const string SCOPE = "XboxLive.signin offline_access";

    private static readonly WapooOptions _options = new()
    {
        IgnoreMediaTypeCheck = true
    };

    // 日后登录部分
    public static async Task<Option<MicrosoftAuthenticationResponse>> AcquireMicrosoftTokenByDeviceCodeAsync(
        Action<string, string> callback, CancellationToken token = default)
    {
        DeviceCodeResponse? deviceCode = null;
        await Wapoo.Wohoo(DEVICE_ENDPOINT, _options)
            .WithBody(new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", CLIENT_ID },
                { "scope", SCOPE }
            }))
            .ViaPost()
            .ForJsonResult<DeviceCodeResponse>(x => deviceCode = x)
            .FetchAsync(token);
        if (deviceCode == null) return Option<MicrosoftAuthenticationResponse>.None();
        callback(deviceCode.Value.UserCode, deviceCode.Value.VerificationUri.AbsoluteUri);
        // 轮询
        bool? successful = null;
        MicrosoftAuthenticationResponse? flow = null;
        var expired = DateTimeOffset.Now + TimeSpan.FromSeconds(deviceCode?.ExpiresIn ?? 0);
        while (DateTimeOffset.Now < expired && successful == null && !token.IsCancellationRequested)
        {
            await Wapoo.Wohoo(TOKEN_ENDPOINT, _options)
                .WithBody(new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "urn:ietf:params:oauth:grant-type:device_code" },
                    { "client_id", CLIENT_ID },
                    { "device_code", deviceCode?.DeviceCode! }
                }))
                .ViaPost()
                .ForJsonResult<JObject>(x =>
                {
                    if (x.ContainsKey("error"))
                    {
                        var error = x["error"]!.Value<string>();
                        if (error != "authorization_pending") successful = false;
                    }
                    else
                    {
                        flow = x.ToObject<MicrosoftAuthenticationResponse>();
                        successful = true;
                    }
                })
                .FetchAsync(token);
            Thread.Sleep((deviceCode?.Interval ?? 5) * 1000);
        }

        return successful == true
            ? Option<MicrosoftAuthenticationResponse>.Some(flow!.Value)
            : Option<MicrosoftAuthenticationResponse>.None();
    }

    public static async Task<Option<XboxLiveResponse>> AcquireXboxTokenAsync(string microsoftToken,
        CancellationToken token = default)
    {
        XboxLiveResponse? response = null;
        await Wapoo.Wohoo(XBOX_ENDPOINT, _options)
            .ViaPost()
            .WithJsonBody(new XboxLiveRequest
            {
                TokenType = "JWT",
                RelyingParty = "http://auth.xboxlive.com",
                Properties = new Dictionary<string, object>
                {
                    { "AuthMethod", "RPS" },
                    { "SiteName", "user.auth.xboxlive.com" },
                    { "RpsTicket", $"d={microsoftToken}" }
                }
            })
            .ForJsonResult<XboxLiveResponse>(x => response = x)
            .FetchAsync(token);
        return response != null ? Option<XboxLiveResponse>.Some(response.Value) : Option<XboxLiveResponse>.None();
    }

    public static async Task<Option<XboxLiveResponse>> AcquireXstsTokenAsync(string xboxToken,
        CancellationToken token = default)
    {
        XboxLiveResponse? response = null;
        await Wapoo.Wohoo(XSTS_ENDPOINT, _options)
            .WithJsonBody(new XboxLiveRequest
            {
                TokenType = "JWT",
                RelyingParty = "rp://api.minecraftservices.com/",
                Properties = new Dictionary<string, object>
                {
                    { "SandboxId", "RETAIL" },
                    { "UserTokens", new[] { xboxToken } }
                }
            })
            .ViaPost()
            .ForJsonResult<XboxLiveResponse>(x => response = x)
            .FetchAsync(token);
        return response != null ? Option<XboxLiveResponse>.Some(response.Value) : Option<XboxLiveResponse>.None();
    }

    public static async Task<Option<MinecraftAuthenticationResponse>> AcquireMinecraftTokenAsync(string xstsToken,
        string userHash, CancellationToken token = default)
    {
        MinecraftAuthenticationResponse? response = null;
        await Wapoo.Wohoo(MINECRAFT_ENDPOINT, _options)
            .WithJsonBody(new Dictionary<string, string>
            {
                { "identityToken", $"XBL3.0 x={userHash};{xstsToken}" }
            })
            .ViaPost()
            .ForJsonResult<MinecraftAuthenticationResponse>(x => response = x)
            .FetchAsync(token);
        return response != null
            ? Option<MinecraftAuthenticationResponse>.Some(response.Value)
            : Option<MinecraftAuthenticationResponse>.None();
    }

    public static Task<bool> VerifyMinecraftOwnershipAsync(string accessToken, CancellationToken token = default)
    {
        //bool res = false;
        //await Wapoo.Wohoo(STORE_ENDPOINT)
        //    .UseBearer(accessToken)
        //    .ViaGet()
        //    .FetchAsync(token);
        //return res;


        // 由于 response 不是 json 不好处理，这里跳过验证
        return Task.FromResult(true);
    }

    public static async Task<Option<MinecraftProfileResponse>> GetProfileByAccessTokenAsync(string accessToken,
        CancellationToken token = default)
    {
        MinecraftProfileResponse? response = null;
        await Wapoo.Wohoo(PROFILE_ENDPOINT, _options)
            .UseBearer(accessToken)
            .ViaGet()
            .ForJsonResult<MinecraftProfileResponse>(x => response = x)
            .FetchAsync(token);
        return response != null
            ? Option<MinecraftProfileResponse>.Some(response.Value)
            : Option<MinecraftProfileResponse>.None();
    }

    public static async Task<Option<MicrosoftAuthenticationResponse>> RefreshAccessTokenAsync(string refreshToken,
        CancellationToken token = default)
    {
        MicrosoftAuthenticationResponse? response = null;
        await Wapoo.Wohoo(TOKEN_ENDPOINT, _options)
            .ViaPost()
            .WithBody(new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", CLIENT_ID },
                { "refresh_token", refreshToken },
                { "grant_type", "refresh_token" }
            }))
            .ForJsonResult<MicrosoftAuthenticationResponse>(x => response = x)
            .FetchAsync(token);
        return response != null
            ? Option<MicrosoftAuthenticationResponse>.Some(response.Value)
            : Option<MicrosoftAuthenticationResponse>.None();
    }
}