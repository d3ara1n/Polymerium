namespace Polymerium.Trident.Models.XboxLiveApi;

public record XboxLiveResponse(
    long? XErr,
    string? Message,
    DateTimeOffset IssueInstant,
    DateTimeOffset NotAfter,
    string Token,
    XboxLiveResponse.Claims DisplayClaims)
{
    public record Claims(Claims.XuiEntry[] Xui)
    {
        public record XuiEntry(string Uhs);
    }
}