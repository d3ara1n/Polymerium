namespace Polymerium.Trident.Models.Microsoft;

public record TokenResponse(
    string? TokenType,
    string? AccessToken,
    string? RefreshToken,
    string? IdToken,
    int ExpiresIn,
    string? Error,
    string? ErrorDescription);