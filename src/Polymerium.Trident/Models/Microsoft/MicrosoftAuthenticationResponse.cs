namespace Polymerium.Trident.Models.Microsoft;

public struct MicrosoftAuthenticationResponse
{
    public string TokenType { get; set; }
    public string Scope { get; set; }
    public int ExpiresIn { get; set; }
    public int ExtExpiresIn { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public string IdToken { get; set; }
    public string Error { get; set; }
    public string ErrorDescription { get; set; }
}