using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Polymerium.Core.Models.Microsoft;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public struct DeviceCodeResponse
{
    public string DeviceCode { get; set; }
    public string UserCode { get; set; }
    public Uri VerificationUri { get; set; }
    public int ExpiresIn { get; set; }
    public int Interval { get; set; }
    public string Message { get; set; }
}