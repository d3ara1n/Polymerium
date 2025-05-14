using Polymerium.Trident.Services;

namespace Polymerium.Trident.Models.MicrosoftApi;

public record AuthenticateRequest(
    string DeviceCode,
    string GrantType = "urn:ietf:params:oauth:grant-type:device_code",
    string ClientId = MicrosoftService.CLIENT_ID);