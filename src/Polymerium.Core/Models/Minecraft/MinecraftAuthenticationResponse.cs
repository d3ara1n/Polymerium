using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Polymerium.Core.Models.Minecraft;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public struct MinecraftAuthenticationResponse
{
    public string Username { get; set; }
    public IEnumerable<string> Roles { get; set; }
    public string AccessToken { get; set; }
    public string TokenType { get; set; }
    public int ExpireIn { get; set; }
}
