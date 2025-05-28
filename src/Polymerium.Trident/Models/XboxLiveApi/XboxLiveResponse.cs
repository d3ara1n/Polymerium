namespace Polymerium.Trident.Models.XboxLiveApi;

public record XboxLiveResponse(
    long? XErr,
    string? Message,
    DateTimeOffset IssueInstant,
    DateTimeOffset NotAfter,
    string Token,
    XboxLiveResponse.Claims DisplayClaims)
{
    #region Nested type: Claims

    public record Claims(Claims.XuiEntry[] Xui)
    {
        #region Nested type: XuiEntry

        public record XuiEntry(string Uhs);

        #endregion
    }

    #endregion
}