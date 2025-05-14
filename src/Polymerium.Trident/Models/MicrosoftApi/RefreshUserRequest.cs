using Polymerium.Trident.Services;

namespace Polymerium.Trident.Models.MicrosoftApi;

public record RefreshUserRequest(
    string RefreshToken,
    string GrantType = "refresh_token",
    string ClientId = MicrosoftService.CLIENT_ID);