namespace Polymerium.Trident.Models.XboxLive
{
    public struct XboxLiveResponse
    {
        public DateTimeOffset IssueInstant { get; set; }
        public DateTimeOffset NotAfter { get; set; }
        public string Token { get; set; }
        public XboxLiveUserAuthencationResponseClaims DisplayClaims { get; set; }

        public struct XboxLiveUserAuthencationResponseClaims
        {
            public IEnumerable<XboxLiveUserAuthenticationResponseXui> Xui { get; set; }

            public struct XboxLiveUserAuthenticationResponseXui
            {
                public string Uhs { get; set; }
            }
        }
    }
}