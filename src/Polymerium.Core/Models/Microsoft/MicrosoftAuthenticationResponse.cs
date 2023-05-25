using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Polymerium.Core.Models.Microsoft;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public struct MicrosoftAuthenticationResponse
{
    public string TokenType { get; set; }
    public string Scope { get; set; }
    public int ExpiresIn { get; set; }
    public int ExtExpiresIn { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public string IdToken { get; set; }
}
