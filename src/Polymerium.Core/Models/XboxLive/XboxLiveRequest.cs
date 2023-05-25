using System.Collections.Generic;

namespace Polymerium.Core.Models.XboxLive;

public struct XboxLiveRequest
{
    public string TokenType { get; set; }
    public string RelyingParty { get; set; }
    public IDictionary<string, object> Properties { get; set; }
}
