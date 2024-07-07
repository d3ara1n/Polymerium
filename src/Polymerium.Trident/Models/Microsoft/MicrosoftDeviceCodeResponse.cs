namespace Polymerium.Trident.Models.Microsoft;

public struct MicrosoftDeviceCodeResponse
{
    public string DeviceCode { get; set; }
    public string UserCode { get; set; }
    public Uri VerificationUri { get; set; }
    public int ExpiresIn { get; set; }
    public int Interval { get; set; }
    public string Message { get; set; }
}