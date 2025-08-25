namespace Polymerium.Trident.Models.XboxLiveApi
{
    public readonly record struct XboxLiveResponse(
        long? XErr,
        string? Message,
        DateTimeOffset IssueInstant,
        DateTimeOffset NotAfter,
        string Token,
        XboxLiveResponse.Claims DisplayClaims)
    {
        #region Nested type: Claims

        public readonly record struct Claims(Claims.XuiEntry[] Xui)
        {
            #region Nested type: XuiEntry

            public readonly record struct XuiEntry(string Uhs);

            #endregion
        }

        #endregion
    }
}
