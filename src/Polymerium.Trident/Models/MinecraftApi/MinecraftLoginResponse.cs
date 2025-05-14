namespace Polymerium.Trident.Models.MinecraftApi;

public record MinecraftLoginResponse(
    string? Error,
    string? ErrorMessage,
    string Username,
    string AccessToken,
    string TokenType,
    int ExipresIn);