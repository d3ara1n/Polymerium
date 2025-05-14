using Polymerium.Trident.Services;
using Refit;

namespace Polymerium.Trident.Models.MicrosoftApi;

public record RefreshUserRequest(
    [property: AliasAs("refresh_token")] string RefreshToken,
    [property: AliasAs("grant_type")] string GrantType = "refresh_token",
    [property: AliasAs("client_id")] string ClientId = MicrosoftService.CLIENT_ID);