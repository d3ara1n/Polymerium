namespace Polymerium.Trident.Models.MicrosoftApi;

public record DeviceCodeResponse(
    string? Message,
    string DeviceCode,
    string UserCode,
    Uri VerificationUri,
    int ExpiresIn,
    int Interval);