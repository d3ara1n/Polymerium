using Polymerium.Trident.Services;
using Refit;

namespace Polymerium.Trident.Models.MicrosoftApi;

public readonly record struct AcquireUserCodeRequest(
    [property: AliasAs("client_id")] string ClientId = MicrosoftService.CLIENT_ID,
    [property: AliasAs("scope")] string Scope = MicrosoftService.SCOPE);