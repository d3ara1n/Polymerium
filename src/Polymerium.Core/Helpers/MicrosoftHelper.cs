using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Wupoo;

namespace Polymerium.Core.Helpers;

public static class MicrosoftHelper
{
    private const string DEVICE_ENDPOINT = "https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode";
    private const string TOKEN_ENDPOINT = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
    private const string PROFILE_ENDPOINT = "https://api.minecraftservices.com/minecraft/profile";
    private const string CLIENT_ID = "66b049dc-22a1-4fd8-a17d-2ccd01332101";

    private const string SCOPE = "XboxLive.signin offline_access";

    // 日后登录部分
    public static async Task AcquireTokenByDeviceCodeAsync(Action<string, string> callback)
    {
        string? deviceCode = null;
        int? interval = null;
        await Wapoo.Wohoo(DEVICE_ENDPOINT)
            .WithBody(new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", CLIENT_ID },
                { "scope", SCOPE }
            }))
            .ForJsonResult<JObject>(x =>
            {
                if (x.ContainsKey("device_code") && x.ContainsKey("user_code") && x.ContainsKey("verification_uri"))
                {
                    deviceCode = x["device_code"]!.Value<string>();
                    var userCode = x["user_code"]!.Value<string>();
                    var verificationUrl = x["verification_uri"]!.Value<string>();
                    interval = x["interval"]!.Value<int>();
                    callback(userCode!, verificationUrl!);
                }
            })
            .FetchAsync();
        // 难搞
    }
}