namespace Polymerium.Trident.Models.MicrosoftApi;

public readonly record struct TokenResponse(
    string? Error,
    string? ErrorDescription,
    string TokenType,
    string AccessToken,
    string RefreshToken,
    string IdToken,
    int ExpiresIn);