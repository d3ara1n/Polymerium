namespace Polymerium.Trident.Models.Microsoft;

public record DeviceCodeResponse(
    string DeviceCode,
    string UserCode,
    Uri VerificationUri,
    int ExpiresIn,
    int Interval,
    string Message);