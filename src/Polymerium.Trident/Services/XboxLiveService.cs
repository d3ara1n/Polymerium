using Polymerium.Trident.Clients;
using Polymerium.Trident.Exceptions;
using Polymerium.Trident.Models.XboxLiveApi;

namespace Polymerium.Trident.Services;

public class XboxLiveService(IXboxLiveClient liveClient, IXboxServiceClient serviceClient)
{
    public const string XBOX_ENDPOINT = "https://user.auth.xboxlive.com";
    public const string XSTS_ENDPOINT = "https://xsts.auth.xboxlive.com";

    public async Task<XboxLiveResponse> AuthenticateForXboxLiveTokenByMicrosoftTokenAsync(string microsoftToken) =>
        EnsureResponseStatus(await liveClient
            .AcquireXboxLiveTokenAsync(new(new($"d={microsoftToken}"),
                "http://auth.xboxlive.com"))
            .ConfigureAwait(false));

    public async Task<XboxLiveResponse> AuthorizeForServiceTokenByXboxLiveTokenAsync(string xboxLiveToken) =>
        EnsureResponseStatus(await serviceClient
            .AcquireMinecraftTokenAsync(new(new([xboxLiveToken]),
                "rp://api.minecraftservices.com/"))
            .ConfigureAwait(false));

    private static XboxLiveResponse EnsureResponseStatus(XboxLiveResponse response)
    {
        if (response.XErr.HasValue)
        {
            var kind = response.XErr.Value switch
            {
                2148916233 => XboxLiveAuthenticationException.ErrorKind.ParentControl,
                2148916238 => XboxLiveAuthenticationException.ErrorKind.UnsupportedRegion,
                _ => XboxLiveAuthenticationException.ErrorKind.Unknown
            };
            throw new XboxLiveAuthenticationException(kind, response.Message ?? "No message provided");
        }

        return response;
    }
}