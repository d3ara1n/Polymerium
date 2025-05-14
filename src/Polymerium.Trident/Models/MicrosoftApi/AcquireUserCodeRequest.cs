using Polymerium.Trident.Services;

namespace Polymerium.Trident.Models.MicrosoftApi;

public record AcquireUserCodeRequest(
    string ClientId = MicrosoftService.CLIENT_ID,
    string Scope = MicrosoftService.SCOPE);