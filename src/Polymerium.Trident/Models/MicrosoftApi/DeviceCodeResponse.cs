namespace Polymerium.Trident.Models.MicrosoftApi
{
    public readonly record struct DeviceCodeResponse(
        string? Message,
        string DeviceCode,
        string UserCode,
        Uri? VerificationUri,
        int ExpiresIn,
        int Interval);
}
