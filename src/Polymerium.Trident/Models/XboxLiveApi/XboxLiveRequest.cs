namespace Polymerium.Trident.Models.XboxLiveApi
{
    public readonly record struct XboxLiveRequest<T>(T Properties, string RelyingParty, string TokenType = "JWT");
}
