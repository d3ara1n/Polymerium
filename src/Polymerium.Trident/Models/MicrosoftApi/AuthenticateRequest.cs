using Polymerium.Trident.Services;
using Refit;

namespace Polymerium.Trident.Models.MicrosoftApi;

public readonly record struct AuthenticateRequest(
    [property: AliasAs("device_code")] string DeviceCode,
    [property: AliasAs("grant_type")] string GrantType = "urn:ietf:params:oauth:grant-type:device_code",
    [property: AliasAs("client_id")] string ClientId = MicrosoftService.CLIENT_ID);