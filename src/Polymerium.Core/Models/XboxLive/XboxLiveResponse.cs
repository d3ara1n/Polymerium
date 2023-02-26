using System;
using System.Collections.Generic;

namespace Polymerium.Core.Models.XboxLive;

public struct XboxLiveResponse
{
    public DateTimeOffset IssueInstant { get; set; }
    public DateTimeOffset NotAfter { get; set; }
    public string Token { get; set; }
    public XboxLiveAuthenticationResponseClaims DisplayClaims { get; set; }

    public struct XboxLiveAuthenticationResponseClaims
    {
        public IEnumerable<XboxLiveAuthenticationResponseXui> Xui { get; set; }

        public struct XboxLiveAuthenticationResponseXui
        {
            public string Uhs { get; set; }
        }
    }
}